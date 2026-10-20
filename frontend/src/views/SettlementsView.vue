<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import api from '../services/api';
import type { User, Service } from '../types';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

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

// Parámetros de cálculo editables en vivo por el administrador
const companyRetentionRate = ref<number>(30.0);
const driverCommissionRate = ref<number>(25.0);
const isrDeduction = ref<number>(873.00);
const tssDeduction = ref<number>(1088.69);
const phoneIncentive = ref<number>(1000.00);
const otherIncentives = ref<number>(0.0);
const otherDeductions = ref<number>(0.0);
const settlementNotes = ref<string>('');

const showParameters = ref(false);

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

// Inicializar selección
onMounted(async () => {
  if (driverList.value.length > 0 && !selectedDriverId.value) {
    selectedDriverId.value = driverList.value[0].id;
  }
  await loadSettlement();
});

watch([selectedDriverId, selectedMonth, selectedYear], async () => {
  if (selectedDriverId.value) {
    // Si cambia el chofer, cargar sus valores predeterminados
    const d = currentDriver.value;
    if (d) {
      driverCommissionRate.value = d.commissionRate || 25.0;
      phoneIncentive.value = (d as any).defaultPhoneIncentive || 1000.0;
      isrDeduction.value = (d as any).defaultIsr || 873.0;
      tssDeduction.value = (d as any).defaultTss || 1088.69;
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
      companyRetentionRate.value = res.companyRetentionRate;
      driverCommissionRate.value = res.driverCommissionRate;
      isrDeduction.value = res.isrDeduction;
      tssDeduction.value = res.tssDeduction;
      phoneIncentive.value = res.phoneIncentive;
      otherIncentives.value = res.otherIncentives || 0;
      otherDeductions.value = res.otherDeductions || 0;
      settlementNotes.value = res.notes || '';
    }
  } catch (error: any) {
    emit('notify', error.message || 'Error al calcular la liquidación.', 'error');
  } finally {
    loading.value = false;
  }
};

// Cálculos reactivos en el frontend en caso de ajustes manuales
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

const calculatedNetPayable = computed(() => {
  const net = calculatedCommissionAmount.value 
    - Number(isrDeduction.value || 0) 
    - Number(tssDeduction.value || 0) 
    + Number(phoneIncentive.value || 0) 
    + Number(otherIncentives.value || 0) 
    - Number(otherDeductions.value || 0);
  return Math.round(net * 100) / 100;
});

// Guardar / Aprobar Planilla en Base de Datos
const onSaveSettlement = async (status: 'Borrador' | 'Aprobado') => {
  if (!selectedDriverId.value) return;

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
      otherIncentives: otherIncentives.value,
      otherDeductions: otherDeductions.value,
      status: status,
      notes: settlementNotes.value
    });

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

  const doc = new jsPDF({
    orientation: 'portrait',
    unit: 'mm',
    format: 'a4'
  });

  const driverName = report.value.driverName || currentDriver.value?.fullName || 'Chofer';
  const driverCode = report.value.driverCode || currentDriver.value?.code || 'CHO';
  const monthName = monthsList.find(m => m.value === selectedMonth.value)?.name || '';
  const titleText = `Reporte de Servicios ${driverName} Mes de ${monthName} ${selectedYear.value}`;

  // Encabezado
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(14);
  doc.setTextColor(21, 128, 61); // Verde institucional
  doc.text('TRANSPORTE TITO', 14, 15);

  doc.setFontSize(9);
  doc.setFont('helvetica', 'italic');
  doc.setTextColor(100, 100, 100);
  doc.text('"Servirle es nuestra prioridad"', 14, 20);

  doc.setFontSize(12);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(20, 20, 20);
  doc.text(titleText, 14, 28);

  // Tabla de Servicios
  const tableData = (report.value.services || []).map((s: any) => [
    s.clientReference || 'Referencia',
    s.serviceTypeName || 'Servicio',
    s.description || '',
    formatDate(s.serviceDate),
    formatCurrency(s.priceCharged),
    driverName
  ]);

  autoTable(doc, {
    startY: 33,
    head: [['ReferenciaCLIENTE', 'TIPO DE SERVICIO', 'DESCRIBA EL SERVICIO REALIZADO', 'FECHA DEL SERVICIO', 'PRECIO DEL SERVICIO', 'Chofer']],
    body: tableData,
    theme: 'grid',
    headStyles: {
      fillColor: [240, 240, 240],
      textColor: [0, 0, 0],
      fontStyle: 'bold',
      fontSize: 8,
      halign: 'left'
    },
    bodyStyles: {
      fontSize: 8,
      textColor: [30, 30, 30]
    },
    columnStyles: {
      0: { cellWidth: 28 },
      1: { cellWidth: 28 },
      2: { cellWidth: 62 },
      3: { cellWidth: 22, halign: 'center' },
      4: { cellWidth: 26, halign: 'right' },
      5: { cellWidth: 22, halign: 'center' }
    },
    styles: {
      overflow: 'linebreak',
      cellPadding: 2
    }
  });

  const finalY = (doc as any).lastAutoTable.finalY + 6;

  // Bloque de Liquidación al pie de página (Lado derecho)
  doc.setFontSize(9);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(0, 0, 0);

  const startX = 110;
  const valueX = 190;
  let currentY = finalY;

  const addLine = (label: string, val: string, isRed = false, isBold = false) => {
    doc.setFont('helvetica', isBold ? 'bold' : 'normal');
    doc.setTextColor(isRed ? 220 : 0, isRed ? 20 : 0, isRed ? 60 : 0);
    doc.text(label, startX, currentY);
    doc.text(val, valueX, currentY, { align: 'right' });
    currentY += 5;
  };

  addLine('Total Bruto Servicios:', formatCurrency(calculatedGrossTotal.value), false, true);
  addLine(`${companyRetentionRate.value}% Retención Empresa:`, `(${formatCurrency(calculatedRetentionAmount.value)})`);
  addLine('Sub Total:', formatCurrency(calculatedSubTotal.value), false, true);
  addLine(`${driverCommissionRate.value}% Comisión Chofer:`, formatCurrency(calculatedCommissionAmount.value));
  addLine('ISR:', `(${formatCurrency(isrDeduction.value)})`, true);
  addLine('TSS:', `(${formatCurrency(tssDeduction.value)})`, true);
  addLine('Mas incentivo Teléfono:', formatCurrency(phoneIncentive.value));

  if (otherIncentives.value > 0) {
    addLine('Otros Incentivos:', formatCurrency(otherIncentives.value));
  }
  if (otherDeductions.value > 0) {
    addLine('Otras Deducciones:', `(${formatCurrency(otherDeductions.value)})`, true);
  }

  // Línea divisoria y Total a Pagar
  doc.setDrawColor(0, 0, 0);
  doc.line(startX, currentY - 2, valueX, currentY - 2);
  currentY += 2;
  doc.setFontSize(11);
  addLine('Total a Pagar:', formatCurrency(calculatedNetPayable.value), false, true);

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

  // Descargar PDF
  const filename = `Reporte_Servicios_${driverName.replace(/\s+/g, '_')}_${monthName}_${selectedYear.value}.pdf`;
  doc.save(filename);
  emit('notify', `PDF generado: ${filename}`, 'success');
};

// Imprimir directamente con estilos del navegador
const printReport = () => {
  window.print();
};

// Enlace directo a WhatsApp del Chofer
const sendViaWhatsApp = () => {
  const driver = currentDriver.value;
  if (!driver) return;

  const phone = (driver.phone || '').replace(/\D/g, '');
  const monthName = monthsList.find(m => m.value === selectedMonth.value)?.name || '';

  const msg = `Hola ${driver.fullName}, te adjuntamos el resumen de tu liquidación de Transporte Tito del mes de ${monthName} ${selectedYear.value}:%0A%0A` +
    `• Total Bruto de Viajes: ${formatCurrency(calculatedGrossTotal.value)}%0A` +
    `• Subtotal Operativo: ${formatCurrency(calculatedSubTotal.value)}%0A` +
    `• Comisión (${driverCommissionRate.value}%): ${formatCurrency(calculatedCommissionAmount.value)}%0A` +
    `• Deducciones (ISR + TSS): (${formatCurrency(Number(isrDeduction.value) + Number(tssDeduction.value))})%0A` +
    `• Incentivo de Teléfono: ${formatCurrency(phoneIncentive.value)}%0A` +
    `━━━━━━━━━━━━━━━━━━%0A` +
    `*TOTAL NETO A PAGAR: ${formatCurrency(calculatedNetPayable.value)}*%0A%0A` +
    `Transporte Tito - "Servirle es nuestra prioridad"`;

  const url = phone ? `https://wa.me/${phone}?text=${msg}` : `https://wa.me/?text=${msg}`;
  window.open(url, '_blank');
};

const formatCurrency = (val: number | string) => {
  const n = Number(val) || 0;
  return n.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`;
};
</script>

<template>
  <div class="settlements-container flex flex-col gap-5 pb-16">
    <!-- ======================================================== -->
    <!-- CABECERA: TÍTULO Y SELECTORES DE PERIODO / CHOFER        -->
    <!-- ======================================================== -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-5 bg-brand-card border border-brand-border rounded-2xl shadow-card print:hidden">
      <div>
        <div class="flex items-center gap-2">
          <span class="text-2xl">📑</span>
          <h1 class="text-xl md:text-2xl font-bold text-white font-heading tracking-wide">
            Liquidación y Planilla de Choferes
          </h1>
        </div>
        <p class="text-xs text-slate-400 mt-1">
          Cálculo financiero mensual, conciliación de servicios y exportación a PDF oficial para Transporte Tito.
        </p>
      </div>

      <!-- Selectores de Chofer, Mes y Año -->
      <div class="flex flex-wrap items-center gap-2.5">
        <!-- Selector de Chofer -->
        <div class="flex flex-col gap-1">
          <label class="text-[11px] font-semibold text-slate-300">Chofer:</label>
          <select 
            v-model="selectedDriverId"
            class="px-3 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-xs outline-none focus:border-brand-green-light cursor-pointer font-medium"
          >
            <option v-for="d in driverList" :key="d.id" :value="d.id">
              {{ d.fullName }} ({{ d.code }})
            </option>
          </select>
        </div>

        <!-- Selector de Mes -->
        <div class="flex flex-col gap-1">
          <label class="text-[11px] font-semibold text-slate-300">Mes:</label>
          <select 
            v-model="selectedMonth"
            class="px-3 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-xs outline-none focus:border-brand-green-light cursor-pointer font-medium"
          >
            <option v-for="m in monthsList" :key="m.value" :value="m.value">
              {{ m.name }}
            </option>
          </select>
        </div>

        <!-- Selector de Año -->
        <div class="flex flex-col gap-1">
          <label class="text-[11px] font-semibold text-slate-300">Año:</label>
          <select 
            v-model="selectedYear"
            class="px-3 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-xs outline-none focus:border-brand-green-light cursor-pointer font-medium"
          >
            <option v-for="y in yearsList" :key="y" :value="y">
              {{ y }}
            </option>
          </select>
        </div>

        <button 
          @click="loadSettlement"
          class="self-end px-3.5 py-2 bg-brand-charcoal hover:bg-brand-border/60 border border-brand-border rounded-xl text-xs font-semibold text-slate-200 transition-all flex items-center gap-1.5 cursor-pointer"
          title="Recalcular"
        >
          <span :class="{'animate-spin': loading}">🔄</span>
          <span class="hidden sm:inline">Recalcular</span>
        </button>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- TARJETAS DE RESUMEN EJECUTIVO                            -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-3 print:hidden">
      <!-- 1. Cantidad de Viajes -->
      <div class="p-4 bg-brand-card border border-brand-border rounded-xl shadow-card flex items-center justify-between">
        <div>
          <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">Viajes Realizados</span>
          <span class="text-xl md:text-2xl font-bold text-white font-heading mt-0.5 block">
            {{ report?.services?.length || 0 }}
          </span>
          <span class="text-[10px] text-slate-500">En este periodo</span>
        </div>
        <span class="text-2xl">🚗</span>
      </div>

      <!-- 2. Total Bruto -->
      <div class="p-4 bg-brand-card border border-brand-border rounded-xl shadow-card flex items-center justify-between">
        <div>
          <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">Total Bruto</span>
          <span class="text-xl md:text-2xl font-bold text-slate-200 font-heading mt-0.5 block">
            RD$ {{ formatCurrency(calculatedGrossTotal) }}
          </span>
          <span class="text-[10px] text-emerald-400">100% Facturado</span>
        </div>
        <span class="text-2xl">💵</span>
      </div>

      <!-- 3. Subtotal Base -->
      <div class="p-4 bg-brand-card border border-brand-border rounded-xl shadow-card flex items-center justify-between">
        <div>
          <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">Subtotal Operativo</span>
          <span class="text-xl md:text-2xl font-bold text-slate-300 font-heading mt-0.5 block">
            RD$ {{ formatCurrency(calculatedSubTotal) }}
          </span>
          <span class="text-[10px] text-slate-400">Base para comisión (70%)</span>
        </div>
        <span class="text-2xl">📊</span>
      </div>

      <!-- 4. Total Neto a Pagar -->
      <div class="p-4 bg-gradient-to-br from-brand-card to-emerald-950/40 border border-emerald-500/40 rounded-xl shadow-card flex items-center justify-between relative overflow-hidden">
        <div class="relative z-10">
          <span class="text-[11px] font-bold text-emerald-400 uppercase tracking-wider block">Neto a Pagar Chofer</span>
          <span class="text-xl md:text-2xl font-extrabold text-white font-heading mt-0.5 block drop-shadow-sm">
            RD$ {{ formatCurrency(calculatedNetPayable) }}
          </span>
          <span class="text-[10px] text-emerald-300/80">Comisión - ISR - TSS + Bono</span>
        </div>
        <span class="text-3xl relative z-10">💰</span>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- BARRA DE ACCIONES: PDF, IMPRIMIR, WHATSAPP, GUARDAR     -->
    <!-- ======================================================== -->
    <div class="flex flex-wrap items-center justify-between gap-3 p-3.5 bg-brand-card/90 border border-brand-border rounded-xl print:hidden">
      <div class="flex items-center gap-2">
        <button 
          @click="showParameters = !showParameters"
          class="px-3 py-1.5 bg-brand-charcoal hover:bg-brand-border/60 border border-brand-border rounded-lg text-xs font-semibold text-slate-300 transition-all flex items-center gap-1.5 cursor-pointer"
        >
          <span>⚙️</span>
          <span>{{ showParameters ? 'Ocultar Parámetros de Retención' : 'Ajustar Deducciones e Incentivos' }}</span>
        </button>

        <span v-if="report?.status" :class="['px-2.5 py-1 rounded-full text-[11px] font-semibold uppercase tracking-wider border', report.status === 'Aprobado' ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300' : 'bg-amber-500/15 border-amber-500/30 text-amber-300']">
          Estado: {{ report.status }}
        </span>
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <!-- Descargar PDF -->
        <button 
          @click="exportToPdf"
          class="px-4 py-2 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white text-xs font-heading font-bold rounded-xl shadow-md shadow-brand-green/30 transition-all flex items-center gap-1.5 cursor-pointer"
        >
          <span>📄</span>
          <span>Descargar PDF Oficial</span>
        </button>

        <!-- Imprimir -->
        <button 
          @click="printReport"
          class="px-3.5 py-2 bg-brand-charcoal hover:bg-slate-800 border border-brand-border rounded-xl text-xs font-semibold text-slate-200 transition-all flex items-center gap-1.5 cursor-pointer"
        >
          <span>🖨️</span>
          <span>Imprimir</span>
        </button>

        <!-- Enviar por WhatsApp -->
        <button 
          @click="sendViaWhatsApp"
          class="px-3.5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer shadow-sm"
        >
          <span>💬</span>
          <span>Enviar WhatsApp</span>
        </button>

        <!-- Guardar / Aprobar Planilla -->
        <button 
          @click="onSaveSettlement('Aprobado')"
          :disabled="saving"
          class="px-3.5 py-2 bg-slate-800 hover:bg-slate-700 border border-slate-600 text-slate-200 rounded-xl text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer"
        >
          <span v-if="saving" class="animate-spin">⏳</span>
          <span>{{ saving ? 'Guardando...' : 'Aprobar Planilla' }}</span>
        </button>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- PANEL DE PARÁMETROS EDITABLES EN VIVO (OPCIONAL)        -->
    <!-- ======================================================== -->
    <div v-if="showParameters" class="p-4 bg-brand-card/90 border border-brand-border rounded-xl grid grid-cols-2 sm:grid-cols-4 md:grid-cols-7 gap-3 text-xs animate-fadeIn print:hidden">
      <div>
        <label class="block text-slate-400 font-semibold mb-1">% Retención Empresa</label>
        <input 
          v-model.number="companyRetentionRate" 
          type="number" 
          step="0.5" 
          class="w-full px-2.5 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-center font-bold"
        />
      </div>
      <div>
        <label class="block text-slate-400 font-semibold mb-1">% Comisión Chofer</label>
        <input 
          v-model.number="driverCommissionRate" 
          type="number" 
          step="0.5" 
          class="w-full px-2.5 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-center font-bold"
        />
      </div>
      <div>
        <label class="block text-red-400 font-semibold mb-1">Deducción ISR (RD$)</label>
        <input 
          v-model.number="isrDeduction" 
          type="number" 
          step="1" 
          class="w-full px-2.5 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-red-300 text-center font-bold"
        />
      </div>
      <div>
        <label class="block text-red-400 font-semibold mb-1">Deducción TSS (RD$)</label>
        <input 
          v-model.number="tssDeduction" 
          type="number" 
          step="0.01" 
          class="w-full px-2.5 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-red-300 text-center font-bold"
        />
      </div>
      <div>
        <label class="block text-emerald-400 font-semibold mb-1">Incentivo Teléfono</label>
        <input 
          v-model.number="phoneIncentive" 
          type="number" 
          step="50" 
          class="w-full px-2.5 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-emerald-300 text-center font-bold"
        />
      </div>
      <div>
        <label class="block text-emerald-400 font-semibold mb-1">Otros Incentivos</label>
        <input 
          v-model.number="otherIncentives" 
          type="number" 
          step="50" 
          class="w-full px-2.5 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-emerald-300 text-center font-bold"
        />
      </div>
      <div>
        <label class="block text-amber-400 font-semibold mb-1">Otras Deducciones</label>
        <input 
          v-model.number="otherDeductions" 
          type="number" 
          step="50" 
          class="w-full px-2.5 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-amber-300 text-center font-bold"
        />
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- PLANILLA DE SERVICIOS (FORMATO OFICIAL TRANSPORTE TITO)  -->
    <!-- ======================================================== -->
    <div class="official-sheet-wrapper bg-white text-black p-6 md:p-8 rounded-2xl shadow-2xl border border-slate-300">
      <!-- Encabezado de la Planilla -->
      <div class="text-center mb-6 pb-4 border-b border-slate-300">
        <h2 class="text-base md:text-lg font-bold text-slate-900 tracking-wide">
          Reporte de Servicios {{ currentDriver?.fullName || report?.driverName }} Mes de {{ monthsList.find(m => m.value === selectedMonth)?.name }} {{ selectedYear }}
        </h2>
        <div class="flex items-center justify-center gap-4 text-xs text-slate-600 mt-1">
          <span><strong>Chofer:</strong> {{ currentDriver?.fullName || report?.driverName }} ({{ currentDriver?.code || report?.driverCode }})</span>
          <span>•</span>
          <span><strong>Teléfono:</strong> {{ currentDriver?.phone || 'Flotilla Tito' }}</span>
          <span>•</span>
          <span><strong>Comisión Asignada:</strong> {{ driverCommissionRate }}%</span>
        </div>
      </div>

      <!-- Tabla Idéntica a la Hoja Física -->
      <div class="overflow-x-auto">
        <table class="w-full border-collapse border border-slate-400 text-xs">
          <thead>
            <tr class="bg-slate-100 font-bold text-slate-800">
              <th class="border border-slate-400 p-2 text-left w-36">ReferenciaCLIENTE</th>
              <th class="border border-slate-400 p-2 text-left w-36">TIPO DE SERVICIO</th>
              <th class="border border-slate-400 p-2 text-left">DESCRIBA EL SERVICIO REALIZADO</th>
              <th class="border border-slate-400 p-2 text-center w-28">FECHA DEL SERVICIO</th>
              <th class="border border-slate-400 p-2 text-right w-32">PRECIO DEL SERVICIO</th>
              <th class="border border-slate-400 p-2 text-center w-28">Chofer</th>
            </tr>
          </thead>
          <tbody>
            <tr 
              v-for="(service, index) in (report?.services || [])" 
              :key="service.id || index"
              class="hover:bg-slate-50 transition-colors"
            >
              <td class="border border-slate-400 p-2 font-medium">{{ service.clientReference || 'Referencia' }}</td>
              <td class="border border-slate-400 p-2">{{ service.serviceTypeName || 'Servicio' }}</td>
              <td class="border border-slate-400 p-2">{{ service.description }}</td>
              <td class="border border-slate-400 p-2 text-center whitespace-nowrap">{{ formatDate(service.serviceDate) }}</td>
              <td class="border border-slate-400 p-2 text-right font-semibold whitespace-nowrap">
                {{ formatCurrency(service.priceCharged) }}
              </td>
              <td class="border border-slate-400 p-2 text-center font-medium">
                {{ currentDriver?.fullName || report?.driverName }}
              </td>
            </tr>

            <!-- Estado Vacío -->
            <tr v-if="!report?.services || report.services.length === 0">
              <td colspan="6" class="border border-slate-400 p-8 text-center text-slate-500">
                <span class="text-2xl block mb-2">📋</span>
                No hay servicios registrados para este chofer en el mes seleccionado.
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ======================================================== -->
      <!-- DESGLOSE FINANCIERO Y CÁLCULOS (PIE DE PLANILLA)         -->
      <!-- ======================================================== -->
      <div class="mt-6 flex flex-col md:flex-row justify-between items-start gap-8">
        <!-- Notas o comentarios -->
        <div class="w-full md:w-1/2 text-xs text-slate-600 print:hidden">
          <label class="block font-bold text-slate-700 mb-1">Observaciones / Notas de la Planilla:</label>
          <textarea 
            v-model="settlementNotes"
            rows="3"
            placeholder="Escribe notas adicionales sobre el cálculo de este mes..."
            class="w-full p-2.5 border border-slate-300 rounded-lg text-slate-800 outline-none focus:border-emerald-600"
          ></textarea>
        </div>

        <!-- Desglose Idéntico a la Hoja Oficial -->
        <div class="w-full md:w-72 self-end text-xs font-medium space-y-1.5 border-t md:border-t-0 pt-4 md:pt-0">
          <!-- Total Bruto -->
          <div class="flex justify-between items-center text-sm font-bold border-b border-slate-400 pb-1">
            <span>Total Servicios Bruto:</span>
            <span>{{ formatCurrency(calculatedGrossTotal) }}</span>
          </div>

          <!-- 30% Retención Empresa -->
          <div class="flex justify-between items-center">
            <span>{{ companyRetentionRate }}% Retención Empresa:</span>
            <span>{{ formatCurrency(calculatedRetentionAmount) }}</span>
          </div>

          <!-- Subtotal -->
          <div class="flex justify-between items-center font-bold border-t border-slate-300 pt-1">
            <span>Sub Total:</span>
            <span>{{ formatCurrency(calculatedSubTotal) }}</span>
          </div>

          <!-- 25% Comisión Chofer -->
          <div class="flex justify-between items-center text-slate-800">
            <span>{{ driverCommissionRate }}% Comisión Chofer:</span>
            <span class="font-bold">{{ formatCurrency(calculatedCommissionAmount) }}</span>
          </div>

          <!-- Deducciones Fiscales -->
          <div class="flex justify-between items-center text-red-600 pt-1">
            <span>ISR:</span>
            <span>({{ formatCurrency(isrDeduction) }})</span>
          </div>

          <div class="flex justify-between items-center text-red-600">
            <span>TSS:</span>
            <span>({{ formatCurrency(tssDeduction) }})</span>
          </div>

          <!-- Incentivo Teléfono -->
          <div class="flex justify-between items-center text-emerald-700">
            <span>Mas incentivo Teléfono:</span>
            <span>{{ formatCurrency(phoneIncentive) }}</span>
          </div>

          <div v-if="otherIncentives > 0" class="flex justify-between items-center text-emerald-700">
            <span>Otros Incentivos:</span>
            <span>{{ formatCurrency(otherIncentives) }}</span>
          </div>

          <div v-if="otherDeductions > 0" class="flex justify-between items-center text-red-600">
            <span>Otras Deducciones:</span>
            <span>({{ formatCurrency(otherDeductions) }})</span>
          </div>

          <!-- Total a Pagar Final -->
          <div class="flex justify-between items-center text-sm md:text-base font-extrabold text-slate-900 border-t-2 border-slate-900 pt-2 mt-2">
            <span>Total a Pagar:</span>
            <span class="text-emerald-700">RD$ {{ formatCurrency(calculatedNetPayable) }}</span>
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
