<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import api from '../services/api';
import type { User } from '../types';
import { parseServiceExpense, toggleServiceExpenseCashStatus } from '../utils/expenses';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import {
  FileSpreadsheet,
  RefreshCw,
  Car,
  Banknote,
  Wallet,
  Receipt,
  Settings2,
  CheckCircle2,
  Share2,
  FileDown,
  Printer,
  MessageCircle,
  AlertTriangle,
  ChevronDown,
  Save,
  Calendar,
  MapPin
} from 'lucide-vue-next';

const props = defineProps<{
  currentUser?: User | null;
  drivers?: User[];
}>();

const emit = defineEmits<{
  (e: 'notify', message: string, type?: string): void;
}>();

// Estado de selección de periodo y chofer
const selectedDriverId = ref<number | string>('');
const selectedMonth = ref<number>(new Date().getMonth() + 1);
const selectedYear = ref<number>(new Date().getFullYear());

const loading = ref(false);
const saving = ref(false);
const report = ref<any>(null);

// Modo de cálculo de deducciones estatutarias (TSS e ISR)
// 'proporcional': TSS (5.91% sobre comisión) e ISR según escala exenta DGII (< RD$ 34,685.00/mes = 0)
// 'exento': Sin retención fija sobre comisión por viaje (ISR = 0, TSS = 0)
// 'manual': Montos fijos editados manualmente por contabilidad
const deductionMode = ref<'proporcional' | 'exento' | 'manual'>('proporcional');

// Parámetros de cálculo editables en vivo por el administrador
const companyRetentionRate = ref<number>(30.0);
const driverCommissionRate = ref<number>(25.0);
const isrDeduction = ref<number>(0.0);
const tssDeduction = ref<number>(0.0);
const phoneIncentive = ref<number>(1000.00);
const expensesReimbursement = ref<number>(0.0);
const otherIncentives = ref<number>(0.0);
const otherDeductions = ref<number>(0.0);
const settlementNotes = ref<string>('');

const showParameters = ref(false);
const showShareMenu = ref(false);

const closeMenus = () => {
  showShareMenu.value = false;
};

onMounted(async () => {
  window.addEventListener('click', closeMenus);
  if (driverList.value.length > 0 && !selectedDriverId.value) {
    selectedDriverId.value = driverList.value[0].id;
  }
  await loadSettlement();
});

onUnmounted(() => {
  window.removeEventListener('click', closeMenus);
});

const monthsList = [
  { value: 1, name: 'Enero' },
  { value: 2, name: 'Febrero' },
  { value: 3, name: 'Marzo' },
  { value: 4, name: 'Abril' },
  { value: 5, name: 'Mayo' },
  { value: 6, name: 'Junio' },
  { value: 7, name: 'Julio' },
  { value: 8, name: 'Agosto' },
  { value: 9, name: 'Septiembre' },
  { value: 10, name: 'Octubre' },
  { value: 11, name: 'Noviembre' },
  { value: 12, name: 'Diciembre' }
];

const yearsList = [2025, 2026, 2027, 2028];

// Lista de choferes disponibles
const driverList = computed(() => {
  return (props.drivers || []).filter(u => (u.role === 'Chofer' || u.role === 'Conductor') && !u.isSuperAdmin);
});

// Chofer actualmente seleccionado
const currentDriver = computed(() => {
  return driverList.value.find(d => String(d.id) === String(selectedDriverId.value)) || null;
});

// Suma de gastos / peajes PENDIENTES de reembolso (se suman al neto de la planilla)
const pendingServicesExpenses = computed(() => {
  if (!report.value?.services) return 0;
  return report.value.services.reduce((sum: number, s: any) => {
    const exp = parseServiceExpense(s.notes, s.id);
    return sum + (!exp.paidInCash ? exp.amount : 0);
  }, 0);
});

// Suma de gastos / peajes YA ENTREGADOS EN EFECTIVO el mismo día (informativo, no suma al neto)
const cashPaidServicesExpenses = computed(() => {
  if (!report.value?.services) return 0;
  return report.value.services.reduce((sum: number, s: any) => {
    const exp = parseServiceExpense(s.notes, s.id);
    return sum + (exp.paidInCash ? exp.amount : 0);
  }, 0);
});

// Mantener sincronizado el reembolso en planilla cuando se conmuta un gasto entre Efectivo y Planilla
watch(pendingServicesExpenses, (newVal) => {
  expensesReimbursement.value = newVal;
});

const onToggleExpenseCash = (service: any) => {
  const isNowCash = toggleServiceExpenseCashStatus(service);
  emit(
    'notify',
    isNowCash
      ? 'Gasto marcado como pagado en efectivo el mismo día (no se suma al neto de planilla).'
      : 'Gasto marcado como pendiente de reembolso (sumado al neto de planilla).',
    'info'
  );
};

// Cálculos reactivos de base
const calculatedGrossTotal = computed(() => {
  if (!report.value || !report.value.services) return 0;
  return report.value.services.reduce((sum: number, s: any) => sum + (Number(s.priceCharged) || 0), 0);
});

const calculatedRetentionAmount = computed(() => {
  return Math.round(calculatedGrossTotal.value * (companyRetentionRate.value / 100) * 100) / 100;
});

const calculatedSubTotal = computed(() => {
  return Math.round((calculatedGrossTotal.value - calculatedRetentionAmount.value) * 100) / 100;
});

const calculatedCommissionAmount = computed(() => {
  return Math.round(calculatedSubTotal.value * (driverCommissionRate.value / 100) * 100) / 100;
});

// Recalcular TSS (5.91% AFP+SFS) e ISR (Escala DGII RD) cuando cambia el modo o la comisión
const applyDeductionMode = () => {
  const comm = calculatedCommissionAmount.value;
  if (deductionMode.value === 'exento' || comm <= 0) {
    tssDeduction.value = 0;
    isrDeduction.value = 0;
  } else if (deductionMode.value === 'proporcional') {
    // TSS laboral RD: 2.87% AFP + 3.04% SFS = 5.91% sobre el ingreso cotizable
    tssDeduction.value = Math.round(comm * 0.0591 * 100) / 100;
    // Escala salarial DGII RD: exento hasta RD$ 34,685.00 mensuales
    const netTaxable = comm - tssDeduction.value;
    if (netTaxable <= 34685) {
      isrDeduction.value = 0;
    } else if (netTaxable <= 52027.42) {
      isrDeduction.value = Math.round((netTaxable - 34685) * 0.15 * 100) / 100;
    } else {
      isrDeduction.value = Math.round((2601.36 + (netTaxable - 52027.42) * 0.20) * 100) / 100;
    }
  }
};

watch([deductionMode, calculatedCommissionAmount], () => {
  if (deductionMode.value !== 'manual') {
    applyDeductionMode();
  }
});

watch([selectedDriverId, selectedMonth, selectedYear], async () => {
  if (selectedDriverId.value) {
    const d = currentDriver.value;
    if (d) {
      driverCommissionRate.value = d.commissionRate || 25.0;
      phoneIncentive.value = (d as any).defaultPhoneIncentive || 1000.0;
    }
    await loadSettlement();
  }
});

// Cargar cálculo de liquidación desde el backend
const loadSettlement = async () => {
  if (!selectedDriverId.value) return;

  try {
    loading.value = true;
    const res = await api.calculateSettlement({
      driverId: selectedDriverId.value,
      month: selectedMonth.value,
      year: selectedYear.value,
      companyRetentionRate: companyRetentionRate.value,
      driverCommissionRate: driverCommissionRate.value,
      isr: isrDeduction.value,
      tss: tssDeduction.value,
      phoneIncentive: phoneIncentive.value,
      otherIncentives: otherIncentives.value,
      otherDeductions: otherDeductions.value
    });

    report.value = res;
    if (res) {
      companyRetentionRate.value = res.companyRetentionRate ?? 30.0;
      driverCommissionRate.value = res.driverCommissionRate ?? (currentDriver.value?.commissionRate || 25.0);
      phoneIncentive.value = res.phoneIncentive ?? 1000.0;
      otherIncentives.value = res.otherIncentives || 0;
      otherDeductions.value = res.otherDeductions || 0;
      settlementNotes.value = res.notes || '';

      // Inicializar reembolso de gastos únicamente con los viáticos/peajes PENDIENTES de pago
      expensesReimbursement.value = pendingServicesExpenses.value;

      // Si el backend devolvió los valores fijos de plantilla (873 y 1088.69) que causan saldo negativo en comisiones bajas,
      // aplicar el cálculo proporcional de ley por defecto
      if (deductionMode.value !== 'manual') {
        applyDeductionMode();
      } else {
        isrDeduction.value = res.isrDeduction || 0;
        tssDeduction.value = res.tssDeduction || 0;
      }
    }
  } catch (error: any) {
    emit('notify', error.message || 'Error al calcular la liquidación.', 'error');
  } finally {
    loading.value = false;
  }
};

const calculatedNetPayable = computed(() => {
  if (calculatedGrossTotal.value === 0 && (!report.value?.services || report.value.services.length === 0)) {
    return 0;
  }
  const net = calculatedCommissionAmount.value 
    - Number(isrDeduction.value || 0) 
    - Number(tssDeduction.value || 0) 
    + Number(phoneIncentive.value || 0) 
    + Number(expensesReimbursement.value || 0)
    + Number(otherIncentives.value || 0) 
    - Number(otherDeductions.value || 0);
  return Math.round(net * 100) / 100;
});

// Estado de validez de la planilla
const isNegativeOrZeroNet = computed(() => calculatedNetPayable.value <= 0);
const isDraft = computed(() => !report.value?.status || report.value.status !== 'Aprobado');
const canApproveSettlement = computed(() => {
  return Boolean(
    !saving.value &&
    report.value?.services?.length > 0 &&
    calculatedNetPayable.value > 0
  );
});

// Guardar / Aprobar Planilla en Base de Datos
const onSaveSettlement = async (status: 'Borrador' | 'Aprobado') => {
  if (!selectedDriverId.value) return;

  if (status === 'Aprobado' && calculatedNetPayable.value <= 0) {
    emit('notify', 'No se puede aprobar una planilla con saldo neto negativo o en cero. Revisa las deducciones.', 'error');
    return;
  }

  try {
    saving.value = true;
    await api.saveSettlement({
      driverId: Number(selectedDriverId.value),
      month: selectedMonth.value,
      year: selectedYear.value,
      companyRetentionRate: companyRetentionRate.value,
      driverCommissionRate: driverCommissionRate.value,
      isrDeduction: isrDeduction.value,
      tssDeduction: tssDeduction.value,
      phoneIncentive: phoneIncentive.value,
      otherIncentives: Number(otherIncentives.value || 0) + Number(expensesReimbursement.value || 0),
      otherDeductions: otherDeductions.value,
      status: status,
      notes: settlementNotes.value
    });

    if (report.value) {
      report.value.status = status;
    }
    emit('notify', `Planilla guardada exitosamente como "${status}".`, 'success');
    await loadSettlement();
  } catch (error: any) {
    emit('notify', error.message || 'Error al guardar la planilla.', 'error');
  } finally {
    saving.value = false;
  }
};

// Generar PDF Oficial de la Planilla (jsPDF + autoTable)
const exportToPdf = () => {
  if (!report.value) return;
  if (calculatedNetPayable.value < 0) {
    emit('notify', 'Corrige el saldo negativo antes de exportar el PDF oficial.', 'error');
    return;
  }

  const doc = new jsPDF({
    orientation: 'portrait',
    unit: 'mm',
    format: 'a4'
  });

  const driverName = report.value.driverName || currentDriver.value?.fullName || 'Chofer';
  const monthName = monthsList.find(m => m.value === selectedMonth.value)?.name || '';
  const titleText = `Liquidación de Servicios — ${driverName} (${monthName} ${selectedYear.value})`;

  // Si está en estado Borrador, dibujar marca de agua diagonal
  if (isDraft.value) {
    doc.saveGraphicsState();
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(54);
    doc.setTextColor(235, 235, 235);
    doc.text('BORRADOR', 105, 160, { align: 'center', angle: 35 });
    doc.restoreGraphicsState();
  }

  // Encabezado
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(14);
  doc.setTextColor(21, 128, 61);
  doc.text('TRANSPORTE TITO', 14, 15);

  doc.setFontSize(9);
  doc.setFont('helvetica', 'italic');
  doc.setTextColor(100, 100, 100);
  doc.text('"Servirle es nuestra prioridad"', 14, 20);

  doc.setFontSize(11.5);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(20, 20, 20);
  doc.text(titleText, 14, 28);

  // Tabla de Servicios con encabezados limpios
  const tableData = (report.value.services || []).map((s: any) => {
    const exp = parseServiceExpense(s.notes, s.id);
    let expLabel = '-';
    if (exp.amount > 0) {
      expLabel = exp.paidInCash
        ? `RD$ ${formatCurrency(exp.amount)} (Efectivo)`
        : `+RD$ ${formatCurrency(exp.amount)} (Planilla)`;
    }
    return [
      s.clientReference || s.clientName || 'Cliente',
      s.serviceTypeName || 'Servicio',
      s.description || '',
      formatDate(s.serviceDate),
      expLabel,
      `RD$ ${formatCurrency(s.priceCharged)}`
    ];
  });

  autoTable(doc, {
    startY: 33,
    head: [['REFERENCIA / CLIENTE', 'TIPO DE SERVICIO', 'DETALLE DEL SERVICIO', 'FECHA', 'GASTOS', 'MONTO COBRADO']],
    body: tableData,
    theme: 'grid',
    headStyles: {
      fillColor: [21, 128, 61],
      textColor: [255, 255, 255],
      fontStyle: 'bold',
      fontSize: 7.5,
      halign: 'left'
    },
    bodyStyles: {
      fontSize: 8,
      textColor: [30, 30, 30]
    },
    columnStyles: {
      0: { cellWidth: 32 },
      1: { cellWidth: 30 },
      2: { cellWidth: 58 },
      3: { cellWidth: 20, halign: 'center' },
      4: { cellWidth: 22, halign: 'right' },
      5: { cellWidth: 26, halign: 'right', fontStyle: 'bold' }
    },
    styles: {
      overflow: 'linebreak',
      cellPadding: 2
    }
  });

  const finalY = (doc as any).lastAutoTable.finalY + 7;

  // Bloque de Liquidación al pie de página
  doc.setFontSize(9);
  const startX = 105;
  const valueX = 195;
  let currentY = finalY;

  const addLine = (label: string, val: string, isRed = false, isBold = false) => {
    doc.setFont('helvetica', isBold ? 'bold' : 'normal');
    doc.setTextColor(isRed ? 200 : 20, isRed ? 30 : 20, isRed ? 45 : 20);
    doc.text(label, startX, currentY);
    doc.text(val, valueX, currentY, { align: 'right' });
    currentY += 5.5;
  };

  addLine('Total Bruto Servicios:', `RD$ ${formatCurrency(calculatedGrossTotal.value)}`, false, true);
  addLine(`${companyRetentionRate.value}% Retención Operativa Empresa:`, `(RD$ ${formatCurrency(calculatedRetentionAmount.value)})`);
  addLine('Subtotal Operativo:', `RD$ ${formatCurrency(calculatedSubTotal.value)}`, false, true);
  addLine(`${driverCommissionRate.value}% Comisión Chofer:`, `RD$ ${formatCurrency(calculatedCommissionAmount.value)}`, false, true);

  if (isrDeduction.value > 0) {
    addLine('Retención ISR:', `(RD$ ${formatCurrency(isrDeduction.value)})`, true);
  } else {
    addLine('Retención ISR (Exento):', 'RD$ 0.00');
  }

  if (tssDeduction.value > 0) {
    addLine('Aporte TSS (5.91%):', `(RD$ ${formatCurrency(tssDeduction.value)})`, true);
  } else {
    addLine('Aporte TSS:', 'RD$ 0.00');
  }

  addLine('Bono por teléfono:', `RD$ ${formatCurrency(phoneIncentive.value)}`);

  if (expensesReimbursement.value > 0) {
    addLine('Reembolso Gastos Pendientes:', `RD$ ${formatCurrency(expensesReimbursement.value)}`);
  }
  if (cashPaidServicesExpenses.value > 0) {
    addLine('Gastos ya entregados en efectivo (Día del viaje):', `RD$ ${formatCurrency(cashPaidServicesExpenses.value)}`);
  }
  if (otherIncentives.value > 0) {
    addLine('Otros Incentivos:', `RD$ ${formatCurrency(otherIncentives.value)}`);
  }
  if (otherDeductions.value > 0) {
    addLine('Otras Deducciones:', `(RD$ ${formatCurrency(otherDeductions.value)})`, true);
  }

  doc.setDrawColor(0, 0, 0);
  doc.line(startX, currentY - 2, valueX, currentY - 2);
  currentY += 2.5;
  doc.setFontSize(11);
  addLine('Total Neto a Pagar:', `RD$ ${formatCurrency(calculatedNetPayable.value)}`, false, true);

  // Líneas de Firmas
  const signatureY = Math.max(currentY + 15, 260);
  doc.setDrawColor(150, 150, 150);
  doc.line(20, signatureY, 80, signatureY);
  doc.line(130, signatureY, 190, signatureY);

  doc.setFontSize(8);
  doc.setFont('helvetica', 'normal');
  doc.setTextColor(80, 80, 80);
  doc.text(`Firma del Chofer (${driverName})`, 25, signatureY + 5);
  doc.text('Aprobado por Administración', 135, signatureY + 5);

  const prefix = isDraft.value ? 'Borrador_Liquidacion' : 'Liquidacion_Oficial';
  const filename = `${prefix}_${driverName.replace(/\s+/g, '_')}_${monthName}_${selectedYear.value}.pdf`;
  doc.save(filename);
  emit('notify', `PDF generado: ${filename}`, 'success');
};

const printReport = () => {
  window.print();
};

const sendViaWhatsApp = () => {
  const driver = currentDriver.value;
  if (!driver) return;

  const phone = (driver.phone || '').replace(/\D/g, '');
  const monthName = monthsList.find(m => m.value === selectedMonth.value)?.name || '';

  const msg = `Hola ${driver.fullName}, te compartimos el resumen de tu liquidación de Transporte Tito (${monthName} ${selectedYear.value}):%0A%0A` +
    `• Total Bruto de Viajes: RD$ ${formatCurrency(calculatedGrossTotal.value)}%0A` +
    `• Subtotal Operativo: RD$ ${formatCurrency(calculatedSubTotal.value)}%0A` +
    `• Comisión (${driverCommissionRate.value}%): RD$ ${formatCurrency(calculatedCommissionAmount.value)}%0A` +
    `• Deducciones (ISR + TSS): (RD$ ${formatCurrency(Number(isrDeduction.value) + Number(tssDeduction.value))})%0A` +
    `• Bono por teléfono: RD$ ${formatCurrency(phoneIncentive.value)}%0A` +
    (expensesReimbursement.value > 0 ? `• Reembolso Gastos Pendientes: +RD$ ${formatCurrency(expensesReimbursement.value)}%0A` : '') +
    (cashPaidServicesExpenses.value > 0 ? `• Gastos ya entregados en efectivo: RD$ ${formatCurrency(cashPaidServicesExpenses.value)} (Pagado)%0A` : '') +
    `━━━━━━━━━━━━━━━━━━%0A` +
    `*TOTAL NETO A PAGAR: RD$ ${formatCurrency(calculatedNetPayable.value)}*%0A%0A` +
    `Transporte Tito - "Servirle es nuestra prioridad"`;

  const url = phone ? `https://wa.me/${phone}?text=${msg}` : `https://wa.me/?text=${msg}`;
  window.open(url, '_blank');
};

const formatCurrency = (val: number | string) => {
  const n = Number(val) || 0;
  return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
};
</script>

<template>
  <div class="settlements-container flex flex-col gap-5 pb-16">
    <!-- ======================================================== -->
    <!-- CABECERA: TÍTULO Y SELECTORES DE PERIODO / CHOFER        -->
    <!-- ======================================================== -->
    <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4 p-5 bg-brand-card border border-brand-border rounded-2xl shadow-card print:hidden">
      <div class="flex items-start gap-3.5">
        <div class="w-11 h-11 rounded-xl bg-brand-green/20 border border-brand-green/40 flex items-center justify-center text-brand-green-light shrink-0">
          <FileSpreadsheet class="w-6 h-6" />
        </div>
        <div>
          <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading tracking-tight">
            Liquidación y Planilla de Choferes
          </h1>
          <p class="text-[13px] text-slate-300 mt-0.5">
            Conciliación mensual de comisiones, reembolso de gastos y aprobación oficial de pagos.
          </p>
        </div>
      </div>

      <!-- Selectores de Chofer, Mes, Año y Botón Recalcular con etiqueta clara -->
      <div class="flex flex-wrap items-end gap-2.5">
        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-slate-300" for="settlement-driver">Chofer:</label>
          <select 
            id="settlement-driver"
            v-model="selectedDriverId"
            class="px-3 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-[13px] outline-none focus:border-brand-green-light cursor-pointer font-semibold"
          >
            <option v-for="d in driverList" :key="d.id" :value="d.id">
              {{ d.fullName }} ({{ d.code }})
            </option>
          </select>
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-slate-300" for="settlement-month">Mes:</label>
          <select 
            id="settlement-month"
            v-model="selectedMonth"
            class="px-3 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-[13px] outline-none focus:border-brand-green-light cursor-pointer font-semibold"
          >
            <option v-for="m in monthsList" :key="m.value" :value="m.value">
              {{ m.name }}
            </option>
          </select>
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-slate-300" for="settlement-year">Año:</label>
          <select 
            id="settlement-year"
            v-model="selectedYear"
            class="px-3 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-[13px] outline-none focus:border-brand-green-light cursor-pointer font-semibold"
          >
            <option v-for="y in yearsList" :key="y" :value="y">
              {{ y }}
            </option>
          </select>
        </div>

        <button 
          type="button"
          @click="loadSettlement"
          class="px-3.5 py-2 bg-brand-charcoal hover:bg-brand-card-hover border border-brand-border rounded-xl text-[13px] font-semibold text-slate-200 hover:text-white transition-all flex items-center gap-2 cursor-pointer"
          title="Recalcular liquidación del período"
        >
          <RefreshCw class="w-4 h-4" :class="{'animate-spin': loading}" />
          <span>Recalcular</span>
        </button>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- ALERTA CRÍTICA SI EL SALDO ES NEGATIVO                   -->
    <!-- ======================================================== -->
    <div 
      v-if="calculatedNetPayable < 0" 
      class="p-4 rounded-2xl bg-red-500/15 border border-red-500/40 text-red-200 flex items-start gap-3 shadow-sm print:hidden"
    >
      <AlertTriangle class="w-6 h-6 text-red-400 shrink-0 mt-0.5" />
      <div class="text-[13px] leading-relaxed">
        <strong class="block text-sm font-bold text-white mb-0.5">
          Atención: Las deducciones superan la comisión ganada (Saldo: -RD$ {{ formatCurrency(Math.abs(calculatedNetPayable)) }})
        </strong>
        <span>
          Con este resultado el chofer no recibiría pago y generaría una <strong>cuenta por cobrar</strong> a favor de la empresa. No es posible aprobar la planilla ni emitir un PDF Oficial mientras el neto sea negativo. Ajusta las deducciones de ISR/TSS a modo <em>Proporcional</em> o <em>Exento</em>.
        </span>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- TARJETAS DE RESUMEN EJECUTIVO                            -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3.5 print:hidden">
      <!-- 1. Cantidad de Viajes -->
      <div class="p-4 bg-brand-card border border-brand-border rounded-xl shadow-card flex items-center justify-between gap-2">
        <div>
          <span class="text-xs font-bold text-slate-300 uppercase tracking-wider block">Viajes Realizados</span>
          <span class="text-2xl font-extrabold text-white font-heading mt-0.5 block">
            {{ report?.services?.length || 0 }}
          </span>
          <span class="text-xs text-slate-300 block">
            Reembolso planilla: <strong class="text-emerald-300">RD$ {{ formatCurrency(expensesReimbursement) }}</strong>
          </span>
          <span v-if="cashPaidServicesExpenses > 0" class="text-[11px] text-slate-400 block">
            Ya pagado en efectivo: RD$ {{ formatCurrency(cashPaidServicesExpenses) }}
          </span>
        </div>
        <div class="w-10 h-10 rounded-xl bg-blue-500/15 border border-blue-500/30 flex items-center justify-center text-blue-400 shrink-0">
          <Car class="w-5 h-5" />
        </div>
      </div>

      <!-- 2. Total Bruto -->
      <div class="p-4 bg-brand-card border border-brand-border rounded-xl shadow-card flex items-center justify-between gap-2">
        <div>
          <span class="text-xs font-bold text-slate-300 uppercase tracking-wider block">Total Bruto</span>
          <span class="text-xl md:text-2xl font-extrabold text-white font-heading mt-0.5 block">
            RD$ {{ formatCurrency(calculatedGrossTotal) }}
          </span>
          <span class="text-xs text-emerald-400 font-medium">100% Facturado</span>
        </div>
        <div class="w-10 h-10 rounded-xl bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shrink-0">
          <Banknote class="w-5 h-5" />
        </div>
      </div>

      <!-- 3. Comisión Chofer (25% del Subtotal 70%) -->
      <div class="p-4 bg-brand-card border border-brand-border rounded-xl shadow-card flex items-center justify-between gap-2">
        <div>
          <span class="text-xs font-bold text-slate-300 uppercase tracking-wider block">Comisión ({{ driverCommissionRate }}%)</span>
          <span class="text-xl md:text-2xl font-extrabold text-slate-100 font-heading mt-0.5 block">
            RD$ {{ formatCurrency(calculatedCommissionAmount) }}
          </span>
          <span class="text-xs text-slate-300">Sobre subtotal RD$ {{ formatCurrency(calculatedSubTotal) }}</span>
        </div>
        <div class="w-10 h-10 rounded-xl bg-white/[0.06] border border-white/[0.12] flex items-center justify-center text-slate-200 shrink-0">
          <Receipt class="w-5 h-5" />
        </div>
      </div>

      <!-- 4. Total Neto a Pagar (Rojo si es negativo, Verde si es positivo) -->
      <div 
        :class="[
          'p-4 rounded-xl shadow-card flex items-center justify-between gap-2 relative overflow-hidden border',
          calculatedNetPayable < 0
            ? 'bg-gradient-to-br from-red-950/60 to-brand-card border-red-500/50'
            : 'bg-gradient-to-br from-brand-card to-emerald-950/50 border-emerald-500/45'
        ]"
      >
        <div class="relative z-10">
          <span 
            class="text-xs font-bold uppercase tracking-wider block"
            :class="calculatedNetPayable < 0 ? 'text-red-300' : 'text-emerald-300'"
          >
            {{ calculatedNetPayable < 0 ? 'Saldo en Contra (Deuda)' : 'Neto a Pagar Chofer' }}
          </span>
          <span 
            class="text-xl md:text-2xl font-extrabold font-heading mt-0.5 block"
            :class="calculatedNetPayable < 0 ? 'text-red-400' : 'text-brand-green-light'"
          >
            {{ calculatedNetPayable < 0 ? `-RD$ ${formatCurrency(Math.abs(calculatedNetPayable))}` : `RD$ ${formatCurrency(calculatedNetPayable)}` }}
          </span>
          <span class="text-xs text-slate-300">
            {{ calculatedNetPayable < 0 ? 'Deducciones exceden ingreso' : 'Comisión - Ley + Bono + Gastos' }}
          </span>
        </div>
        <div 
          class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0 relative z-10 border"
          :class="calculatedNetPayable < 0 ? 'bg-red-500/20 border-red-500/40 text-red-300' : 'bg-brand-green/20 border-brand-green/40 text-brand-green-light'"
        >
          <AlertTriangle v-if="calculatedNetPayable < 0" class="w-5 h-5" />
          <Wallet v-else class="w-5 h-5" />
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- BARRA DE ACCIONES CON JERARQUÍA CLARA Y MENÚ COMPARTIR    -->
    <!-- ======================================================== -->
    <div class="flex flex-wrap items-center justify-between gap-3 p-4 bg-brand-card border border-brand-border rounded-2xl print:hidden">
      <div class="flex items-center gap-2.5 flex-wrap">
        <button 
          type="button"
          @click="showParameters = !showParameters"
          class="px-3.5 py-2 bg-brand-charcoal hover:bg-brand-card-hover border border-brand-border rounded-xl text-[13px] font-semibold text-slate-200 hover:text-white transition-all flex items-center gap-2 cursor-pointer"
        >
          <Settings2 class="w-4 h-4 text-brand-green-light" />
          <span>{{ showParameters ? 'Ocultar Parámetros' : 'Ajustar Deducciones y Bonos' }}</span>
        </button>

        <span 
          :class="[
            'px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider border inline-flex items-center gap-1.5',
            report?.status === 'Aprobado'
              ? 'bg-emerald-500/15 border-emerald-500/35 text-emerald-300'
              : 'bg-amber-500/15 border-amber-500/35 text-amber-300'
          ]"
        >
          <span>Estado: {{ report?.status || 'Borrador' }}</span>
        </span>
      </div>

      <!-- Acciones agrupadas: Guardar Borrador, Compartir/Exportar y Acción Principal "Aprobar Planilla" -->
      <div class="flex items-center gap-2.5 flex-wrap">
        <button 
          type="button"
          @click="onSaveSettlement('Borrador')"
          :disabled="saving"
          class="px-3.5 py-2 bg-brand-charcoal hover:bg-white/[0.08] border border-brand-border text-slate-200 rounded-xl text-[13px] font-semibold transition-all flex items-center gap-1.5 cursor-pointer"
        >
          <Save class="w-4 h-4" />
          <span>Guardar Borrador</span>
        </button>

        <!-- Menú agrupado Compartir / Exportar (PDF, Imprimir, WhatsApp) -->
        <div class="relative" @click.stop>
          <button 
            type="button"
            @click="showShareMenu = !showShareMenu"
            class="px-3.5 py-2 bg-brand-charcoal hover:bg-white/[0.08] border border-brand-border text-slate-100 rounded-xl text-[13px] font-semibold transition-all flex items-center gap-2 cursor-pointer"
          >
            <Share2 class="w-4 h-4 text-brand-green-light" />
            <span>Compartir / Exportar</span>
            <ChevronDown class="w-3.5 h-3.5 text-slate-300" />
          </button>

          <div 
            v-if="showShareMenu" 
            class="absolute right-0 top-full mt-2 w-56 bg-brand-card border border-brand-border-strong rounded-2xl shadow-elevated p-1.5 z-50 animate-fadeIn"
          >
            <button 
              type="button"
              :disabled="calculatedNetPayable < 0"
              class="w-full text-left px-3 py-2.5 text-[13px] font-semibold text-slate-100 hover:bg-white/[0.08] disabled:opacity-40 disabled:cursor-not-allowed rounded-xl flex items-center gap-2.5 transition-colors cursor-pointer"
              @click="exportToPdf(); showShareMenu = false"
            >
              <FileDown class="w-4 h-4 text-brand-green-light" />
              <span>{{ isDraft ? 'Descargar PDF (Borrador)' : 'Descargar PDF Oficial' }}</span>
            </button>

            <button 
              type="button"
              class="w-full text-left px-3 py-2.5 text-[13px] font-semibold text-slate-100 hover:bg-white/[0.08] rounded-xl flex items-center gap-2.5 transition-colors cursor-pointer"
              @click="printReport(); showShareMenu = false"
            >
              <Printer class="w-4 h-4 text-blue-400" />
              <span>Imprimir Planilla</span>
            </button>

            <button 
              type="button"
              class="w-full text-left px-3 py-2.5 text-[13px] font-semibold text-emerald-300 hover:bg-emerald-500/10 rounded-xl flex items-center gap-2.5 transition-colors cursor-pointer"
              @click="sendViaWhatsApp(); showShareMenu = false"
            >
              <MessageCircle class="w-4 h-4 text-emerald-400" />
              <span>Enviar por WhatsApp</span>
            </button>
          </div>
        </div>

        <!-- Acción Primaria: Aprobar Planilla (Bloqueada si el neto es <= 0) -->
        <button 
          type="button"
          @click="onSaveSettlement('Aprobado')"
          :disabled="!canApproveSettlement"
          :title="!canApproveSettlement ? 'No se puede aprobar una planilla sin viajes o con saldo neto negativo' : 'Aprobar planilla oficial'"
          :class="[
            'px-4 py-2 rounded-xl text-[13px] font-heading font-bold transition-all flex items-center gap-2',
            canApproveSettlement
              ? 'bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white shadow-md shadow-brand-green/30 cursor-pointer'
              : 'bg-white/[0.06] border border-white/[0.12] text-slate-400 cursor-not-allowed'
          ]"
        >
          <CheckCircle2 class="w-4 h-4" />
          <span>{{ saving ? 'Guardando...' : 'Aprobar Planilla' }}</span>
        </button>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- PANEL DE PARÁMETROS Y RÉGIMEN DE DEDUCCIONES (OPCIONAL)   -->
    <!-- ======================================================== -->
    <div v-if="showParameters" class="p-4 md:p-5 bg-brand-card border border-brand-border rounded-2xl flex flex-col gap-4 text-[13px] animate-fadeIn print:hidden">
      <!-- Selector de Régimen para evitar ISR/TSS desproporcionados -->
      <div class="flex items-center justify-between flex-wrap gap-3 pb-3 border-b border-brand-border/40">
        <div>
          <span class="font-bold text-white block">Régimen de Deducciones (TSS e ISR)</span>
          <span class="text-xs text-slate-300">Selecciona cómo se calculan las retenciones sobre la comisión del chofer</span>
        </div>
        <div class="flex items-center bg-brand-black/80 border border-brand-border rounded-xl p-1 gap-1 flex-wrap">
          <button
            type="button"
            :class="['px-3 py-1.5 rounded-lg text-xs font-semibold cursor-pointer transition-all', deductionMode === 'proporcional' ? 'bg-brand-green text-white' : 'text-slate-300 hover:text-white']"
            @click="deductionMode = 'proporcional'"
          >
            Proporcional Ley (TSS 5.91% / Escala DGII)
          </button>
          <button
            type="button"
            :class="['px-3 py-1.5 rounded-lg text-xs font-semibold cursor-pointer transition-all', deductionMode === 'exento' ? 'bg-brand-green text-white' : 'text-slate-300 hover:text-white']"
            @click="deductionMode = 'exento'"
          >
            Comisión Pura (Sin ISR/TSS)
          </button>
          <button
            type="button"
            :class="['px-3 py-1.5 rounded-lg text-xs font-semibold cursor-pointer transition-all', deductionMode === 'manual' ? 'bg-brand-green text-white' : 'text-slate-300 hover:text-white']"
            @click="deductionMode = 'manual'"
          >
            Manual (Contador)
          </button>
        </div>
      </div>

      <div class="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-8 gap-3">
        <div>
          <label class="block text-slate-300 font-semibold mb-1 text-xs">% Retención Emp.</label>
          <input 
            v-model.number="companyRetentionRate" 
            type="number" 
            step="0.5" 
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-white text-center font-bold"
          />
        </div>
        <div>
          <label class="block text-slate-300 font-semibold mb-1 text-xs">% Comisión Chofer</label>
          <input 
            v-model.number="driverCommissionRate" 
            type="number" 
            step="0.5" 
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-white text-center font-bold"
          />
        </div>
        <div>
          <label class="block text-red-300 font-semibold mb-1 text-xs">ISR (RD$)</label>
          <input 
            v-model.number="isrDeduction" 
            type="number" 
            step="1" 
            @input="deductionMode = 'manual'"
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-red-300 text-center font-bold"
          />
        </div>
        <div>
          <label class="block text-red-300 font-semibold mb-1 text-xs">TSS (RD$)</label>
          <input 
            v-model.number="tssDeduction" 
            type="number" 
            step="0.01" 
            @input="deductionMode = 'manual'"
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-red-300 text-center font-bold"
          />
        </div>
        <div>
          <label class="block text-emerald-300 font-semibold mb-1 text-xs">Bono por teléfono</label>
          <input 
            v-model.number="phoneIncentive" 
            type="number" 
            step="50" 
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-emerald-300 text-center font-bold"
          />
        </div>
        <div>
          <label class="block text-emerald-300 font-semibold mb-1 text-xs">Reembolso Gastos</label>
          <input 
            v-model.number="expensesReimbursement" 
            type="number" 
            step="50" 
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-emerald-300 text-center font-bold"
          />
        </div>
        <div>
          <label class="block text-emerald-300 font-semibold mb-1 text-xs">Otros Incentivos</label>
          <input 
            v-model.number="otherIncentives" 
            type="number" 
            step="50" 
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-emerald-300 text-center font-bold"
          />
        </div>
        <div>
          <label class="block text-amber-300 font-semibold mb-1 text-xs">Otras Deducciones</label>
          <input 
            v-model.number="otherDeductions" 
            type="number" 
            step="50" 
            class="w-full px-2.5 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-amber-300 text-center font-bold"
          />
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- PLANILLA DE SERVICIOS (VISTA PREVIA CON MARCA DE AGUA)   -->
    <!-- ======================================================== -->
    <div class="official-sheet-wrapper relative overflow-hidden bg-white text-slate-900 p-4 sm:p-6 md:p-8 rounded-2xl shadow-2xl border border-slate-300">
      <!-- Marca de Agua "BORRADOR" mientras no esté aprobada -->
      <div 
        v-if="isDraft" 
        class="pointer-events-none select-none absolute inset-0 flex items-center justify-center z-0 overflow-hidden"
        aria-hidden="true"
      >
        <span class="text-5xl sm:text-7xl md:text-8xl font-extrabold uppercase tracking-widest text-slate-900/[0.06] -rotate-24 border-4 border-slate-900/[0.06] px-8 py-3 rounded-2xl">
          BORRADOR
        </span>
      </div>

      <div class="relative z-10">
        <!-- Encabezado de la Planilla -->
        <div class="text-center mb-5 pb-4 border-b border-slate-300">
          <div class="inline-flex items-center gap-2 px-3 py-0.5 rounded-full text-xs font-bold mb-1.5" :class="isDraft ? 'bg-amber-100 text-amber-800 border border-amber-300' : 'bg-emerald-100 text-emerald-800 border border-emerald-300'">
            <span>{{ isDraft ? 'DOCUMENTO BORRADOR — PENDIENTE DE APROBACIÓN' : 'PLANILLA OFICIAL APROBADA' }}</span>
          </div>
          <h2 class="text-base md:text-lg font-extrabold text-slate-900 tracking-tight">
            Reporte de Servicios — {{ currentDriver?.fullName || report?.driverName }} ({{ monthsList.find(m => m.value === selectedMonth)?.name }} {{ selectedYear }})
          </h2>
          <div class="flex items-center justify-center flex-wrap gap-x-4 gap-y-1 text-xs text-slate-700 mt-1.5">
            <span><strong>Chofer:</strong> {{ currentDriver?.fullName || report?.driverName }} ({{ currentDriver?.code || report?.driverCode }})</span>
            <span class="hidden sm:inline">•</span>
            <span><strong>Teléfono:</strong> {{ currentDriver?.phone || 'Flotilla Tito' }}</span>
            <span class="hidden sm:inline">•</span>
            <span><strong>Comisión Asignada:</strong> {{ driverCommissionRate }}%</span>
          </div>
        </div>

        <!-- 1. TABLA EN ESCRITORIO (Desktop: hidden md:block) -->
        <div class="hidden md:block">
          <table class="w-full border-collapse border border-slate-300 text-xs">
            <thead>
              <tr class="bg-slate-100 font-bold text-slate-800">
                <th class="border border-slate-300 p-2.5 text-left">Referencia / Cliente</th>
                <th class="border border-slate-300 p-2.5 text-left">Tipo de Servicio</th>
                <th class="border border-slate-300 p-2.5 text-left">Detalle del Servicio Realizado</th>
                <th class="border border-slate-300 p-2.5 text-center w-28">Fecha</th>
                <th class="border border-slate-300 p-2.5 text-right w-32">Gastos / Peajes</th>
                <th class="border border-slate-300 p-2.5 text-right w-36">Monto Cobrado</th>
              </tr>
            </thead>
            <tbody>
              <tr 
                v-for="(service, index) in (report?.services || [])" 
                :key="service.id || index"
                class="hover:bg-slate-50 transition-colors"
              >
                <td class="border border-slate-300 p-2.5 font-semibold text-slate-900">
                  {{ service.clientReference || service.clientName || 'Cliente' }}
                </td>
                <td class="border border-slate-300 p-2.5 text-slate-800">
                  {{ service.serviceTypeName || 'Servicio' }}
                </td>
                <td class="border border-slate-300 p-2.5 text-slate-700">
                  <div>{{ service.description }}</div>
                  <div v-if="service.notes" class="text-[11px] text-slate-500 mt-0.5">
                    Nota: {{ parseServiceExpense(service.notes, service.id).detail }}
                  </div>
                </td>
                <td class="border border-slate-300 p-2.5 text-center whitespace-nowrap text-slate-700">
                  {{ formatDate(service.serviceDate) }}
                </td>
                <td class="border border-slate-300 p-2.5 text-right whitespace-nowrap text-slate-700">
                  <button
                    v-if="parseServiceExpense(service.notes, service.id).amount > 0"
                    type="button"
                    @click="onToggleExpenseCash(service)"
                    :title="parseServiceExpense(service.notes, service.id).paidInCash
                      ? 'Entregado en efectivo el mismo día (No suma en planilla). Clic para cambiar a Pendiente en planilla.'
                      : 'Pendiente de reembolso (Suma en planilla). Clic si ya se entregó en efectivo.'"
                    :class="[
                      'inline-flex flex-col items-end px-2 py-1 rounded-lg border text-[11px] font-bold transition-all cursor-pointer print:border-none print:p-0',
                      parseServiceExpense(service.notes, service.id).paidInCash
                        ? 'bg-slate-200/80 hover:bg-slate-300/80 text-slate-700 border-slate-300'
                        : 'bg-emerald-100 hover:bg-emerald-200 text-emerald-800 border-emerald-300'
                    ]"
                  >
                    <span>
                      {{ parseServiceExpense(service.notes, service.id).paidInCash ? 'RD$' : '+RD$' }}
                      {{ formatCurrency(parseServiceExpense(service.notes, service.id).amount) }}
                    </span>
                    <span class="text-[10px] font-semibold opacity-85">
                      {{ parseServiceExpense(service.notes, service.id).paidInCash ? '💵 Pagado efectivo' : '⏳ En planilla' }}
                    </span>
                  </button>
                  <span v-else class="text-slate-400">-</span>
                </td>
                <td class="border border-slate-300 p-2.5 text-right font-bold text-slate-900 whitespace-nowrap">
                  RD$ {{ formatCurrency(service.priceCharged) }}
                </td>
              </tr>

              <tr v-if="!report?.services || report.services.length === 0">
                <td colspan="6" class="border border-slate-300 p-8 text-center text-slate-500">
                  No hay servicios registrados para este chofer en el mes seleccionado.
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- 2. TARJETAS POR SERVICIO EN MÓVIL (Mobile: md:hidden, sin tabla apretada ni scroll horizontal) -->
        <div class="md:hidden flex flex-col gap-2.5">
          <div 
            v-if="!report?.services || report.services.length === 0"
            class="p-6 text-center text-sm text-slate-500 border border-slate-200 rounded-xl bg-slate-50"
          >
            No hay servicios registrados para este chofer en el mes seleccionado.
          </div>

          <div
            v-for="(service, index) in (report?.services || [])"
            :key="service.id || index"
            class="p-3.5 rounded-xl border border-slate-300 bg-slate-50/80 flex flex-col gap-2 text-xs"
          >
            <div class="flex items-center justify-between border-b border-slate-200 pb-1.5">
              <span class="font-semibold text-slate-700 flex items-center gap-1">
                <Calendar class="w-3.5 h-3.5 text-emerald-700" />
                {{ formatDate(service.serviceDate) }}
              </span>
              <span class="font-extrabold text-sm text-slate-900">
                RD$ {{ formatCurrency(service.priceCharged) }}
              </span>
            </div>

            <div class="flex flex-col gap-1">
              <div class="text-[13px] text-slate-900">
                <span class="text-slate-600 font-medium">Cliente: </span>
                <strong class="font-bold">{{ service.clientReference || service.clientName || 'Cliente' }}</strong>
              </div>
              <div class="text-slate-800 font-medium">
                {{ service.serviceTypeName || 'Servicio' }}
              </div>
              <div v-if="service.description" class="text-slate-600 flex items-start gap-1">
                <MapPin class="w-3.5 h-3.5 text-slate-500 shrink-0 mt-0.5" />
                <span>{{ service.description }}</span>
              </div>
              <div v-if="service.notes" class="mt-1 px-2.5 py-1.5 rounded-lg bg-slate-200/70 text-slate-800 flex items-center justify-between gap-2">
                <span class="truncate">{{ parseServiceExpense(service.notes, service.id).detail }}</span>
                <button
                  v-if="parseServiceExpense(service.notes, service.id).amount > 0"
                  type="button"
                  @click="onToggleExpenseCash(service)"
                  :class="[
                    'px-2 py-0.5 rounded border text-[11px] font-bold shrink-0 cursor-pointer transition-all',
                    parseServiceExpense(service.notes, service.id).paidInCash
                      ? 'bg-slate-300 text-slate-800 border-slate-400'
                      : 'bg-emerald-100 text-emerald-800 border-emerald-300'
                  ]"
                >
                  {{ parseServiceExpense(service.notes, service.id).paidInCash
                    ? `💵 Efectivo: RD$ ${formatCurrency(parseServiceExpense(service.notes, service.id).amount)}`
                    : `⏳ Planilla: +RD$ ${formatCurrency(parseServiceExpense(service.notes, service.id).amount)}` }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- ======================================================== -->
        <!-- DESGLOSE FINANCIERO Y CÁLCULOS (PIE DE PLANILLA)         -->
        <!-- ======================================================== -->
        <div class="mt-6 flex flex-col md:flex-row justify-between items-start gap-6">
          <!-- Notas o comentarios -->
          <div class="w-full md:w-1/2 text-xs text-slate-700 print:hidden">
            <label class="block font-bold text-slate-800 mb-1.5 text-[13px]">Observaciones / Notas de la Planilla:</label>
            <textarea 
              v-model="settlementNotes"
              rows="3"
              placeholder="Escribe notas adicionales sobre la conciliación de este mes..."
              class="w-full p-3 border border-slate-300 rounded-xl text-slate-900 text-[13px] outline-none focus:border-emerald-600"
            ></textarea>
          </div>

          <!-- Desglose Financiero Claro -->
          <div class="w-full md:w-80 self-end text-[13px] font-medium space-y-2 bg-slate-50 p-4 rounded-xl border border-slate-200">
            <div class="flex justify-between items-center text-sm font-bold text-slate-900 border-b border-slate-300 pb-1.5">
              <span>Total Bruto Servicios:</span>
              <span>RD$ {{ formatCurrency(calculatedGrossTotal) }}</span>
            </div>

            <div class="flex justify-between items-center text-slate-700">
              <span>{{ companyRetentionRate }}% Retención Empresa:</span>
              <span>(RD$ {{ formatCurrency(calculatedRetentionAmount) }})</span>
            </div>

            <div class="flex justify-between items-center font-bold text-slate-900 border-t border-slate-300 pt-1.5">
              <span>Subtotal Operativo:</span>
              <span>RD$ {{ formatCurrency(calculatedSubTotal) }}</span>
            </div>

            <div class="flex justify-between items-center text-slate-900 bg-emerald-50 px-2 py-1 rounded border border-emerald-200">
              <span>{{ driverCommissionRate }}% Comisión Chofer:</span>
              <span class="font-extrabold text-emerald-800">RD$ {{ formatCurrency(calculatedCommissionAmount) }}</span>
            </div>

            <!-- Deducciones Fiscales -->
            <div class="flex justify-between items-center" :class="isrDeduction > 0 ? 'text-red-600' : 'text-slate-500'">
              <span>ISR {{ isrDeduction === 0 ? '(Exento DGII)' : '' }}:</span>
              <span>{{ isrDeduction > 0 ? `(RD$ ${formatCurrency(isrDeduction)})` : 'RD$ 0.00' }}</span>
            </div>

            <div class="flex justify-between items-center" :class="tssDeduction > 0 ? 'text-red-600' : 'text-slate-500'">
              <span>TSS {{ deductionMode === 'proporcional' ? '(5.91%)' : '' }}:</span>
              <span>{{ tssDeduction > 0 ? `(RD$ ${formatCurrency(tssDeduction)})` : 'RD$ 0.00' }}</span>
            </div>

            <!-- Bono por teléfono (ortografía corregida) -->
            <div class="flex justify-between items-center text-emerald-700 font-semibold">
              <span>Bono por teléfono:</span>
              <span>+RD$ {{ formatCurrency(phoneIncentive) }}</span>
            </div>

            <!-- Reembolso de gastos / peajes pendientes en planilla -->
            <div v-if="expensesReimbursement > 0" class="flex justify-between items-center text-emerald-700 font-semibold">
              <span>Reembolso Gastos Pendientes:</span>
              <span>+RD$ {{ formatCurrency(expensesReimbursement) }}</span>
            </div>

            <!-- Gastos ya entregados en efectivo el día del servicio (informativo, no suma al neto) -->
            <div v-if="cashPaidServicesExpenses > 0" class="flex justify-between items-center text-slate-500 text-xs bg-slate-100 px-2 py-1 rounded border border-slate-200">
              <span>Gastos ya pagados en efectivo:</span>
              <span class="font-semibold">RD$ {{ formatCurrency(cashPaidServicesExpenses) }} (Entregado)</span>
            </div>

            <div v-if="otherIncentives > 0" class="flex justify-between items-center text-emerald-700 font-semibold">
              <span>Otros Incentivos:</span>
              <span>+RD$ {{ formatCurrency(otherIncentives) }}</span>
            </div>

            <div v-if="otherDeductions > 0" class="flex justify-between items-center text-red-600 font-semibold">
              <span>Otras Deducciones:</span>
              <span>(RD$ {{ formatCurrency(otherDeductions) }})</span>
            </div>

            <!-- Total a Pagar Final (Rojo si es negativo, Verde si es positivo) -->
            <div class="flex justify-between items-center text-base font-extrabold border-t-2 border-slate-900 pt-2.5 mt-2">
              <span class="text-slate-900">{{ calculatedNetPayable < 0 ? 'Saldo en Contra:' : 'Total a Pagar:' }}</span>
              <span :class="calculatedNetPayable < 0 ? 'text-red-600' : 'text-emerald-700'">
                {{ calculatedNetPayable < 0 ? `-RD$ ${formatCurrency(Math.abs(calculatedNetPayable))}` : `RD$ ${formatCurrency(calculatedNetPayable)}` }}
              </span>
            </div>
          </div>
        </div>

        <!-- Firmas en Formato Impreso -->
        <div class="hidden print:flex justify-between items-end mt-16 pt-8 border-t border-slate-300 text-xs">
          <div class="text-center w-64">
            <div class="border-b border-slate-400 mb-2"></div>
            <span class="font-bold">Firma del Chofer ({{ currentDriver?.fullName }})</span>
          </div>
          <div class="text-center w-64">
            <div class="border-b border-slate-400 mb-2"></div>
            <span class="font-bold">Autorizado por Administración</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@media print {
  .official-sheet-wrapper {
    box-shadow: none !important;
    border: none !important;
    padding: 0 !important;
    margin: 0 !important;
    width: 100% !important;
  }
}
</style>
