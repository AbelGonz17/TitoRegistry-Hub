<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import type { User, Service } from '../types';
import { parseServiceExpense, toggleServiceExpenseCashStatus } from '../utils/expenses';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import {
  Car,
  RefreshCw,
  FileDown,
  Search,
  ChevronDown,
  Check,
  Banknote,
  Receipt,
  TrendingUp,
  Calendar,
  MapPin,
  ClipboardList,
  Pencil,
  X,
  Users
} from 'lucide-vue-next';

const props = defineProps<{
  services?: Service[];
  loading?: boolean;
  currentUser?: User | null;
  drivers?: User[];
}>();

const emit = defineEmits<{
  (e: 'open-new-service'): void;
  (e: 'edit-service', service: Service): void;
  (e: 'refresh'): void;
  (e: 'notify', message: string, type?: string): void;
}>();

// Estados de filtro
const selectedDriverId = ref<string | number>('all');
const activeFilter = ref<string>('mes'); // 'todos' | 'hoy' | 'semana' | 'mes' | 'mes-anterior' | 'rango'
const customDateFrom = ref<string>('');
const customDateTo = ref<string>('');
const searchQuery = ref<string>('');
const exportingPdf = ref<boolean>(false);

// Estado del Dropdown de Chofer con Búsqueda (Escalable a 15-50+ choferes)
const showDriverDropdown = ref(false);
const driverSearchQuery = ref('');

const closeDropdown = () => {
  showDriverDropdown.value = false;
};

onMounted(() => {
  window.addEventListener('click', closeDropdown);
});

onUnmounted(() => {
  window.removeEventListener('click', closeDropdown);
});

// Lista filtrada de choferes disponibles
const driversList = computed(() => {
  return (props.drivers || []).filter(d => 
    d.role === 'Chofer' || 
    d.role?.includes('Conductor') || 
    (!d.role && !d.isSuperAdmin)
  );
});

const filteredDriversDropdown = computed(() => {
  if (!driverSearchQuery.value.trim()) return driversList.value;
  const q = driverSearchQuery.value.toLowerCase().trim();
  return driversList.value.filter(d =>
    (d.fullName || '').toLowerCase().includes(q) ||
    (d.code || '').toLowerCase().includes(q) ||
    (d.department || '').toLowerCase().includes(q)
  );
});

const selectDriver = (id: string | number) => {
  selectedDriverId.value = id;
  showDriverDropdown.value = false;
  driverSearchQuery.value = '';
};

// Chofer actualmente seleccionado
const currentDriver = computed(() => {
  if (selectedDriverId.value === 'all') return null;
  return driversList.value.find(d => String(d.id) === String(selectedDriverId.value)) || null;
});

const selectedDriverLabel = computed(() => {
  if (selectedDriverId.value === 'all') return 'Flota Completa (Todos)';
  return currentDriver.value ? `${currentDriver.value.fullName} (${currentDriver.value.code || 'CHO'})` : 'Chofer Seleccionado';
});

const onToggleExpenseCash = (srv: Service) => {
  const isNowCash = toggleServiceExpenseCashStatus(srv);
  emit(
    'notify',
    isNowCash
      ? 'Gasto marcado como pagado en efectivo el mismo día.'
      : 'Gasto marcado como pendiente de reembolso en planilla.',
    'info'
  );
};

// Servicios filtrados por chofer, período y texto
const filteredServices = computed(() => {
  let list = props.services || [];

  // 1. Filtro por chofer
  if (selectedDriverId.value !== 'all') {
    const targetId = Number(selectedDriverId.value);
    list = list.filter(s => (s.driverId === targetId || s.employeeId === targetId));
  }

  // 2. Filtro por fecha / período
  const now = new Date();
  if (activeFilter.value === 'hoy') {
    const todayStr = now.toISOString().slice(0, 10);
    list = list.filter(s => s.serviceDate?.slice(0, 10) === todayStr);
  } else if (activeFilter.value === 'semana') {
    const weekAgo = new Date();
    weekAgo.setDate(now.getDate() - 7);
    list = list.filter(s => new Date(s.serviceDate) >= weekAgo);
  } else if (activeFilter.value === 'mes') {
    const monthAgo = new Date();
    monthAgo.setMonth(now.getMonth() - 1);
    list = list.filter(s => new Date(s.serviceDate) >= monthAgo);
  } else if (activeFilter.value === 'mes-anterior') {
    const startLastMonth = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    const endLastMonth = new Date(now.getFullYear(), now.getMonth(), 0, 23, 59, 59);
    list = list.filter(s => {
      const d = new Date(s.serviceDate);
      return d >= startLastMonth && d <= endLastMonth;
    });
  } else if (activeFilter.value === 'rango') {
    if (customDateFrom.value) {
      list = list.filter(s => (s.serviceDate || '').slice(0, 10) >= customDateFrom.value);
    }
    if (customDateTo.value) {
      list = list.filter(s => (s.serviceDate || '').slice(0, 10) <= customDateTo.value);
    }
  }

  // 3. Búsqueda por texto libre
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(s => 
      s.clientName?.toLowerCase().includes(q) ||
      s.clientReference?.toLowerCase().includes(q) ||
      s.clientAddress?.toLowerCase().includes(q) ||
      s.description?.toLowerCase().includes(q) ||
      s.serviceTypeName?.toLowerCase().includes(q) ||
      s.serviceCode?.toLowerCase().includes(q) ||
      s.notes?.toLowerCase().includes(q) ||
      s.driverName?.toLowerCase().includes(q) ||
      s.employeeName?.toLowerCase().includes(q)
    );
  }

  return list;
});

// Métricas del conjunto filtrado
const totalServicesCount = computed(() => filteredServices.value.length);

const totalBilled = computed(() => {
  return filteredServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0);
});

const averagePerService = computed(() => {
  if (totalServicesCount.value === 0) return 0;
  return totalBilled.value / totalServicesCount.value;
});

// Total real de viáticos, peajes y gastos declarados en los viajes filtrados
const totalDietasEstimated = computed(() => {
  return filteredServices.value.reduce((sum, s) => {
    return sum + parseServiceExpense(s.notes, s.id).amount;
  }, 0);
});

const totalPendingExpenses = computed(() => {
  return filteredServices.value.reduce((sum, s) => {
    const exp = parseServiceExpense(s.notes, s.id);
    return sum + (!exp.paidInCash ? exp.amount : 0);
  }, 0);
});

const totalCashExpenses = computed(() => {
  return filteredServices.value.reduce((sum, s) => {
    const exp = parseServiceExpense(s.notes, s.id);
    return sum + (exp.paidInCash ? exp.amount : 0);
  }, 0);
});

const periodLabel = computed(() => {
  switch (activeFilter.value) {
    case 'hoy': return 'Hoy';
    case 'semana': return 'Últimos 7 días';
    case 'mes': return 'Este Mes';
    case 'mes-anterior': return 'Mes Anterior';
    case 'rango': 
      return `Del ${customDateFrom.value || 'inicio'} al ${customDateTo.value || 'hoy'}`;
    default: return 'Histórico Completo';
  }
});

const formatDate = (dateStr?: string) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
};

const formatMoney = (val: number | string) => {
  const num = Number(val) || 0;
  return 'RD$ ' + num.toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
};

const getClientName = (srv: Service) => {
  return srv.clientName || srv.clientReference || 'Cliente Corporativo';
};

// Generar informe oficial PDF
const exportReport = () => {
  if (filteredServices.value.length === 0) {
    emit('notify', 'No hay servicios en el período para generar el informe.', 'warning');
    return;
  }

  try {
    exportingPdf.value = true;
    const doc = new jsPDF({
      orientation: 'landscape',
      unit: 'mm',
      format: 'a4'
    });

    const isAll = selectedDriverId.value === 'all';
    const driverLabel = currentDriver.value?.fullName || 'Flota Completa';
    const driverCode = currentDriver.value?.code || (isAll ? 'FLOTA COMPLETA' : 'CHO-001');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(16);
    doc.setTextColor(21, 128, 61);
    doc.text('TRANSPORTE TITO', 14, 15);

    doc.setFontSize(8);
    doc.setFont('helvetica', 'italic');
    doc.setTextColor(110, 110, 110);
    doc.text('"Servirle es nuestra prioridad" — Operaciones de Transporte y Logística — Rep. Dom.', 14, 20);

    doc.setFontSize(13);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(20, 25, 22);
    const title = isAll 
      ? 'INFORME GENERAL DE SERVICIOS — FLOTA COMPLETA' 
      : `INFORME DETALLADO DE SERVICIOS — ${driverLabel.toUpperCase()}`;
    doc.text(title, 14, 28);

    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(70, 70, 70);
    doc.text(`Conductor: ${driverLabel} (${driverCode})`, 14, 34);
    doc.text(`Período Consultado: ${periodLabel.value}`, 14, 39);

    const nowStr = new Date().toLocaleString('es-DO', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
    doc.text(`Fecha de Emisión: ${nowStr}`, 175, 34);
    doc.text(`Generado por: ${props.currentUser?.fullName || 'Administración'}`, 175, 39);

    doc.setFillColor(243, 248, 245);
    doc.setDrawColor(187, 222, 201);
    doc.roundedRect(14, 43, 269, 12, 2, 2, 'FD');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9.5);
    doc.setTextColor(30, 30, 30);
    doc.text(`Total de Servicios: ${filteredServices.value.length} viajes`, 20, 50.5);

    if (totalDietasEstimated.value > 0) {
      doc.setTextColor(60, 60, 60);
      doc.text(`Gastos / Viáticos: ${formatMoney(totalDietasEstimated.value)}`, 95, 50.5);
    }

    doc.setTextColor(21, 128, 61);
    doc.text(
      `Total Facturado / Cobrado: ${formatMoney(totalBilled.value)}`,
      175,
      50.5
    );

    const tableData = filteredServices.value.map((s, index) => [
      index + 1,
      s.serviceCode || `#${s.id}`,
      formatDate(s.serviceDate),
      s.driverName || s.employeeName || driverLabel,
      getClientName(s),
      s.serviceTypeName || 'Servicio',
      s.description || '-',
      s.notes || '-',
      formatMoney(s.priceCharged)
    ]);

    autoTable(doc, {
      startY: 59,
      head: [[
        '#',
        'CÓDIGO',
        'FECHA',
        'CONDUCTOR',
        'CLIENTE / EMPRESA',
        'RUTA / SERVICIO',
        'DETALLE DE SERVICIO',
        'GASTOS / OBSERVACIONES',
        'COBRADO'
      ]],
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
        fontSize: 7.5,
        textColor: [35, 35, 35]
      },
      columnStyles: {
        0: { cellWidth: 8, halign: 'center' },
        1: { cellWidth: 18, halign: 'center' },
        2: { cellWidth: 20, halign: 'center' },
        3: { cellWidth: 28 },
        4: { cellWidth: 34 },
        5: { cellWidth: 34 },
        6: { cellWidth: 52 },
        7: { cellWidth: 46 },
        8: { cellWidth: 29, halign: 'right', fontStyle: 'bold' }
      },
      foot: [[
        { content: 'TOTAL GENERAL FACTURADO:', colSpan: 8, styles: { halign: 'right', fontStyle: 'bold', fontSize: 8.5 } },
        { content: formatMoney(totalBilled.value), styles: { halign: 'right', fontStyle: 'bold', fontSize: 8.5, textColor: [21, 128, 61] } }
      ]],
      footStyles: {
        fillColor: [238, 245, 241],
        textColor: [0, 0, 0]
      },
      styles: {
        overflow: 'linebreak',
        cellPadding: 2
      },
      didDrawPage: (data) => {
        const pageCount = (doc as any).internal.getNumberOfPages();
        doc.setFontSize(7.5);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(140, 140, 140);
        doc.text(
          `Transporte Tito — Informe de Operaciones y Servicios — Página ${data.pageNumber} de ${pageCount}`,
          14,
          doc.internal.pageSize.height - 7
        );
      }
    });

    const safeName = driverLabel.replace(/[^a-zA-Z0-9]/g, '_');
    const todayFile = new Date().toISOString().slice(0, 10);
    doc.save(`Informe_Servicios_${safeName}_${todayFile}.pdf`);
    emit('notify', `Informe de ${driverLabel} exportado exitosamente.`, 'success');
  } catch (err: any) {
    console.error('Error generando PDF:', err);
    emit('notify', 'Hubo un error al generar el informe en PDF.', 'error');
  } finally {
    exportingPdf.value = false;
  }
};
</script>

<template>
  <div class="flex flex-col gap-5 max-w-[1300px] mx-auto animate-fadeIn pb-12">
    <!-- ======================================================== -->
    <!-- 1. CABECERA PRINCIPAL: Título + Generar Informe PDF       -->
    <!-- ======================================================== -->
    <div class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-card/95 via-brand-card to-brand-black/95 border border-brand-border p-5 md:p-6 shadow-card">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div class="flex items-center gap-3.5">
          <div class="w-12 h-12 rounded-2xl bg-brand-green/20 border border-brand-green/40 flex items-center justify-center text-brand-green-light shadow-glow shrink-0">
            <Car class="w-6 h-6" />
          </div>
          <div>
            <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold bg-brand-green/15 text-brand-green-light border border-brand-green/30 uppercase tracking-wider mb-1">
              <span>Supervisión de Flota</span>
              <span class="opacity-60">•</span>
              <span>Reportes Oficiales</span>
            </div>
            <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading leading-tight">
              Servicios por Chofer
            </h1>
            <p class="text-[13px] text-slate-300 mt-0.5">
              Consulta los viajes registrados por cada conductor, audita gastos y genera informes en PDF.
            </p>
          </div>
        </div>

        <!-- Acciones Principales: Actualizar y Generar informe PDF -->
        <div class="flex items-center gap-2.5 flex-wrap">
          <button 
            type="button"
            class="px-3.5 py-2 bg-brand-charcoal hover:bg-brand-card-hover border border-brand-border rounded-xl text-slate-200 hover:text-white text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer shadow-sm"
            @click="$emit('refresh')"
            :disabled="loading"
            title="Actualizar listado de servicios"
          >
            <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" />
            <span>Actualizar</span>
          </button>

          <button 
            type="button"
            class="inline-flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-emerald-600 to-brand-green hover:from-emerald-500 hover:to-brand-green-light text-white text-[13px] font-heading font-bold rounded-xl shadow-md hover:shadow-lg transition-all cursor-pointer active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed"
            @click="exportReport"
            :disabled="filteredServices.length === 0 || exportingPdf"
            :title="filteredServices.length === 0 ? 'No hay servicios para exportar' : 'Generar informe oficial en PDF'"
          >
            <FileDown class="w-4 h-4" />
            <span>{{ exportingPdf ? 'Generando PDF...' : 'Generar informe PDF' }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 2. SELECTOR DE CONDUCTOR ESCALABLE (DROPDOWN CON BÚSQUEDA)-->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-2xl p-4 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div class="flex flex-col sm:flex-row sm:items-center gap-3 flex-1">
        <div class="flex items-center gap-2 shrink-0">
          <Users class="w-4 h-4 text-brand-green-light" />
          <span class="text-xs font-bold text-slate-200 uppercase tracking-wider">Conductor a auditar:</span>
        </div>

        <!-- Combobox / Dropdown con búsqueda integrada para soportar decenas de choferes -->
        <div class="relative w-full sm:max-w-sm" @click.stop>
          <button
            type="button"
            class="w-full px-3.5 py-2.5 bg-brand-charcoal hover:bg-brand-card-hover border border-brand-border rounded-xl text-left text-[13px] font-semibold text-white flex items-center justify-between gap-2 cursor-pointer transition-all"
            @click="showDriverDropdown = !showDriverDropdown"
          >
            <div class="flex items-center gap-2 truncate">
              <Car class="w-4 h-4 text-brand-green-light shrink-0" />
              <span class="truncate">{{ selectedDriverLabel }}</span>
            </div>
            <ChevronDown class="w-4 h-4 text-slate-300 shrink-0 transition-transform" :class="{ 'rotate-180': showDriverDropdown }" />
          </button>

          <div
            v-if="showDriverDropdown"
            class="absolute left-0 right-0 top-full mt-2 bg-brand-card border border-brand-border-strong rounded-2xl shadow-elevated p-2 z-50 animate-fadeIn"
          >
            <!-- Buscador interno de chofer -->
            <div class="relative mb-2">
              <Search class="w-4 h-4 text-slate-300 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
              <input
                v-model="driverSearchQuery"
                type="text"
                placeholder="Buscar chofer por nombre o código..."
                class="w-full pl-9 pr-7 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-[13px] text-white placeholder-slate-400 outline-none focus:border-brand-green-light"
              />
              <button
                v-if="driverSearchQuery"
                type="button"
                class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-300 hover:text-white cursor-pointer"
                @click="driverSearchQuery = ''"
              >
                <X class="w-3.5 h-3.5" />
              </button>
            </div>

            <div class="max-h-60 overflow-y-auto flex flex-col gap-1">
              <button
                type="button"
                class="w-full px-3 py-2 rounded-xl text-left text-[13px] font-semibold flex items-center justify-between transition-colors cursor-pointer"
                :class="selectedDriverId === 'all' ? 'bg-brand-green/20 text-brand-green-light' : 'text-slate-200 hover:bg-white/[0.06]'"
                @click="selectDriver('all')"
              >
                <span>Flota Completa ({{ driversList.length }} choferes)</span>
                <Check v-if="selectedDriverId === 'all'" class="w-4 h-4 text-brand-green-light" />
              </button>

              <button
                v-for="d in filteredDriversDropdown"
                :key="d.id"
                type="button"
                class="w-full px-3 py-2 rounded-xl text-left text-[13px] font-semibold flex items-center justify-between transition-colors cursor-pointer"
                :class="String(selectedDriverId) === String(d.id) ? 'bg-brand-green/20 text-brand-green-light' : 'text-slate-200 hover:bg-white/[0.06]'"
                @click="selectDriver(d.id)"
              >
                <div class="flex items-center gap-2 truncate">
                  <span class="truncate">{{ d.fullName }}</span>
                  <span class="font-mono text-xs text-slate-300">({{ d.code || 'CHO' }})</span>
                </div>
                <Check v-if="String(selectedDriverId) === String(d.id)" class="w-4 h-4 text-brand-green-light shrink-0" />
              </button>

              <div v-if="filteredDriversDropdown.length === 0" class="py-4 text-center text-xs text-slate-300">
                No se encontraron choferes con "{{ driverSearchQuery }}"
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Resumen compacto del chofer activo -->
      <div v-if="currentDriver" class="flex items-center gap-3 text-[13px] text-slate-200 bg-brand-black/40 px-3.5 py-2 rounded-xl border border-white/[0.06]">
        <span><strong>Código:</strong> {{ currentDriver.code || 'CHO-001' }}</span>
        <span class="opacity-40">•</span>
        <span><strong>Comisión:</strong> {{ currentDriver.commissionRate || 25 }}%</span>
        <span v-if="currentDriver.phone" class="opacity-40 hidden sm:inline">•</span>
        <span v-if="currentDriver.phone" class="hidden sm:inline">{{ currentDriver.phone }}</span>
      </div>
      <div v-else class="text-[13px] text-slate-300">
        Mostrando todos los conductores registrados ({{ driversList.length }})
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 3. TARJETAS DE MÉTRICAS (SIN COLOR AMARILLO DE ALERTA)   -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3.5">
      <!-- Métrica 1: Viajes Realizados -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-4 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-300 text-xs mb-1">
          <span class="uppercase font-bold tracking-wider">Viajes Realizados</span>
          <Car class="w-4 h-4 text-blue-400" />
        </div>
        <div class="text-2xl font-extrabold text-white font-heading">
          {{ totalServicesCount }}
        </div>
        <div class="text-xs text-slate-300 mt-1">
          En {{ periodLabel.toLowerCase() }}
        </div>
      </div>

      <!-- Métrica 2: Total Facturado / Cobrado -->
      <div class="bg-gradient-to-br from-green-950/40 via-brand-card to-brand-card border border-brand-green/40 rounded-xl p-4 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-200 text-xs mb-1">
          <span class="uppercase font-bold tracking-wider">Total Facturado</span>
          <Banknote class="w-4 h-4 text-brand-green-light" />
        </div>
        <div class="text-xl md:text-2xl font-extrabold text-white font-heading">
          {{ formatMoney(totalBilled) }}
        </div>
        <div class="text-xs text-brand-green-light mt-1 font-medium">
          Cobrado a clientes
        </div>
      </div>

      <!-- Métrica 3: Dietas / Gastos (alimentado por peajes y viáticos, con desglose Pendiente vs Efectivo) -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-4 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-300 text-xs mb-1">
          <span class="uppercase font-bold tracking-wider">Dietas / Gastos</span>
          <Receipt class="w-4 h-4 text-slate-200" />
        </div>
        <div class="text-xl md:text-2xl font-extrabold text-white font-heading">
          {{ formatMoney(totalDietasEstimated) }}
        </div>
        <div v-if="totalDietasEstimated > 0" class="text-[11px] text-slate-300 mt-1 flex items-center gap-2 flex-wrap">
          <span class="text-emerald-300 font-semibold">Planilla: {{ formatMoney(totalPendingExpenses) }}</span>
          <span class="opacity-50">•</span>
          <span>Efectivo: {{ formatMoney(totalCashExpenses) }}</span>
        </div>
        <div v-else class="text-xs text-slate-300 mt-1">
          Peajes, comidas y parqueos
        </div>
      </div>

      <!-- Métrica 4: Promedio por Viaje -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-4 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-300 text-xs mb-1">
          <span class="uppercase font-bold tracking-wider">Promedio / Viaje</span>
          <TrendingUp class="w-4 h-4 text-emerald-400" />
        </div>
        <div class="text-xl md:text-2xl font-extrabold text-slate-100 font-heading">
          {{ formatMoney(averagePerService) }}
        </div>
        <div class="text-xs text-slate-300 mt-1">
          Ingreso medio por servicio
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 4. BARRA DE FILTROS DESPLAZABLE HORIZONTALMENTE          -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-3.5 shadow-sm flex flex-col lg:flex-row lg:items-center justify-between gap-3">
      <!-- Contenedor desplazable horizontalmente en una sola línea (nunca deja "Rango" huérfano) -->
      <div class="flex items-center gap-2 overflow-x-auto scrollbar-thin pb-1 lg:pb-0">
        <span class="text-xs text-slate-300 font-bold uppercase tracking-wider shrink-0">Período:</span>
        <div class="inline-flex bg-brand-black/90 border border-brand-border rounded-xl p-0.5 gap-0.5 flex-nowrap shrink-0">
          <button 
            v-for="period in [
              { key: 'todos', label: 'Todos' },
              { key: 'hoy', label: 'Hoy' },
              { key: 'semana', label: 'Semana' },
              { key: 'mes', label: 'Este Mes' },
              { key: 'mes-anterior', label: 'Mes Anterior' },
              { key: 'rango', label: 'Rango' }
            ]"
            :key="period.key"
            type="button"
            :class="[
              'px-3 py-1.5 rounded-lg text-[13px] font-semibold transition-all cursor-pointer whitespace-nowrap shrink-0',
              activeFilter === period.key ? 'bg-brand-green text-white shadow-sm' : 'text-slate-300 hover:text-white'
            ]" 
            @click="activeFilter = period.key"
          >
            {{ period.label }}
          </button>
        </div>

        <!-- Inputs de Rango Personalizado -->
        <div v-if="activeFilter === 'rango'" class="inline-flex items-center gap-1.5 bg-brand-charcoal border border-brand-border rounded-xl px-2.5 py-1 text-[13px] shrink-0 animate-fadeIn">
          <input 
            v-model="customDateFrom" 
            type="date" 
            class="bg-transparent text-white text-[13px] outline-none cursor-pointer"
            title="Fecha Inicial"
          />
          <span class="text-slate-300 text-xs">a</span>
          <input 
            v-model="customDateTo" 
            type="date" 
            class="bg-transparent text-white text-[13px] outline-none cursor-pointer"
            title="Fecha Final"
          />
        </div>
      </div>

      <!-- Buscador por texto -->
      <div class="relative flex items-center min-w-[220px] md:min-w-[280px] w-full lg:w-auto">
        <Search class="w-4 h-4 text-slate-300 absolute left-3 pointer-events-none" />
        <input 
          v-model="searchQuery" 
          type="text" 
          class="w-full pl-9 pr-8 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-[13px] placeholder-slate-400 focus:border-brand-green-light outline-none transition-all" 
          placeholder="Buscar cliente, ruta, código..."
        />
        <button v-if="searchQuery" type="button" class="absolute right-2.5 text-slate-300 hover:text-white cursor-pointer" @click="searchQuery = ''">
          <X class="w-4 h-4" />
        </button>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 5. TABLA Y LISTADO DETALLADO DE SERVICIOS                -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-2xl p-4 md:p-5 shadow-card">
      <div class="flex items-center justify-between pb-3 mb-3 border-b border-brand-border/40">
        <div class="flex items-center gap-2">
          <h2 class="text-base font-bold text-white font-heading">
            Detalle de Viajes ({{ filteredServices.length }})
          </h2>
          <span class="text-[13px] text-slate-300">— {{ selectedDriverLabel }}</span>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="py-12 text-center flex flex-col items-center justify-center gap-2.5">
        <div class="w-7 h-7 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-slate-300">Cargando servicios de choferes...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="filteredServices.length === 0" class="py-12 text-center flex flex-col items-center justify-center gap-2.5 max-w-md mx-auto">
        <ClipboardList class="w-10 h-10 text-slate-400 mb-1" />
        <h3 class="text-base font-bold text-white">No hay servicios registrados</h3>
        <p class="text-sm text-slate-300">
          No se encontraron viajes para <strong>{{ selectedDriverLabel }}</strong> en el período seleccionado ({{ periodLabel }}).
        </p>
      </div>

      <!-- Tabla para Escritorio (Desktop: md:block) -->
      <div v-else class="hidden md:block overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="border-b border-brand-border/50 text-xs font-bold text-slate-300 uppercase tracking-wider">
              <th class="py-3 px-3">Código</th>
              <th class="py-3 px-3">Chofer</th>
              <th class="py-3 px-3">Fecha</th>
              <th class="py-3 px-3">Cliente / Ref.</th>
              <th class="py-3 px-3">Ruta / Servicio</th>
              <th class="py-3 px-3">Detalle</th>
              <th class="py-3 px-3">Gastos / Notas</th>
              <th class="py-3 px-3">Estado</th>
              <th class="py-3 px-3 text-right">Cobrado</th>
              <th class="py-3 px-3 text-center">Acción</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-brand-border/25 text-[13px]">
            <tr v-for="srv in filteredServices" :key="srv.id" class="hover:bg-white/[0.03] transition-colors">
              <td class="py-3 px-3">
                <span class="inline-block px-2 py-0.5 bg-white/[0.06] border border-white/[0.1] rounded font-mono text-xs text-slate-200 font-medium">
                  {{ srv.serviceCode || `#${srv.id}` }}
                </span>
              </td>
              <td class="py-3 px-3">
                <div class="flex items-center gap-1.5">
                  <Car class="w-4 h-4 text-brand-green-light shrink-0" />
                  <span class="font-semibold text-white">{{ srv.driverName || srv.employeeName || selectedDriverLabel }}</span>
                </div>
              </td>
              <td class="py-3 px-3 text-slate-200 whitespace-nowrap">{{ formatDate(srv.serviceDate) }}</td>
              <td class="py-3 px-3">
                <div class="font-semibold text-white">{{ getClientName(srv) }}</div>
                <div v-if="srv.clientPhone" class="text-xs text-slate-300">{{ srv.clientPhone }}</div>
              </td>
              <td class="py-3 px-3">
                <div class="font-medium text-slate-100">{{ srv.serviceTypeName }}</div>
              </td>
              <td class="py-3 px-3">
                <div class="text-slate-200 truncate max-w-xs" :title="srv.description || srv.clientAddress">
                  {{ srv.description || srv.clientAddress || '-' }}
                </div>
              </td>
              <td class="py-3 px-3">
                <div v-if="srv.notes" class="flex flex-col items-start gap-1">
                  <div class="inline-flex items-center gap-1.5 text-xs text-slate-200 bg-white/[0.06] border border-white/[0.12] rounded-lg px-2.5 py-1 max-w-xs truncate" :title="srv.notes">
                    <Receipt class="w-3.5 h-3.5 text-slate-300 shrink-0" />
                    <span v-if="parseServiceExpense(srv.notes, srv.id).amount > 0" class="truncate">
                      Gastos: <strong class="text-white">{{ formatMoney(parseServiceExpense(srv.notes, srv.id).amount) }}</strong> ({{ parseServiceExpense(srv.notes, srv.id).detail }})
                    </span>
                    <span v-else class="truncate">{{ parseServiceExpense(srv.notes, srv.id).detail }}</span>
                  </div>
                  <button
                    v-if="parseServiceExpense(srv.notes, srv.id).amount > 0"
                    type="button"
                    @click="onToggleExpenseCash(srv)"
                    :title="parseServiceExpense(srv.notes, srv.id).paidInCash ? 'Clic para cambiar a Pendiente en planilla' : 'Clic si ya se entregó en efectivo el mismo día'"
                    :class="[
                      'px-2 py-0.5 rounded-full text-[11px] font-bold border transition-all cursor-pointer',
                      parseServiceExpense(srv.notes, srv.id).paidInCash
                        ? 'bg-slate-500/20 border-slate-400/40 text-slate-200 hover:bg-slate-500/30'
                        : 'bg-emerald-500/15 border-emerald-500/35 text-emerald-300 hover:bg-emerald-500/25'
                    ]"
                  >
                    {{ parseServiceExpense(srv.notes, srv.id).paidInCash ? '💵 Pagado en efectivo' : '⏳ Pendiente planilla' }}
                  </button>
                </div>
                <span v-else class="text-xs text-slate-400">-</span>
              </td>
              <td class="py-3 px-3">
                <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                  {{ srv.statusName || 'Completado' }}
                </span>
              </td>
              <td class="py-3 px-3 text-right font-heading font-bold text-white whitespace-nowrap">
                {{ formatMoney(srv.priceCharged) }}
              </td>
              <td class="py-3 px-3 text-center">
                <button
                  type="button"
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-white/[0.06] hover:bg-brand-green/20 border border-white/[0.12] hover:border-brand-green/40 text-xs font-semibold text-slate-200 hover:text-brand-green-light transition-all cursor-pointer"
                  @click="$emit('edit-service', srv)"
                  title="Editar servicio"
                >
                  <Pencil class="w-3.5 h-3.5 text-brand-green-light" />
                  <span>Editar</span>
                </button>
              </td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="border-t border-brand-border/60 bg-white/[0.02] text-[13px] font-bold text-white">
              <td colspan="8" class="py-3 px-3 text-right uppercase tracking-wider text-slate-300 font-heading">
                Total Facturado ({{ filteredServices.length }} viajes):
              </td>
              <td class="py-3 px-3 text-right text-brand-green-light font-heading text-sm font-extrabold whitespace-nowrap">
                {{ formatMoney(totalBilled) }}
              </td>
              <td></td>
            </tr>
          </tfoot>
        </table>
      </div>

      <!-- Vista de Tarjetas para Móviles (Mobile: md:hidden) -->
      <div v-if="!loading && filteredServices.length > 0" class="md:hidden flex flex-col gap-3">
        <div 
          v-for="srv in filteredServices" 
          :key="srv.id" 
          class="bg-brand-charcoal/90 border border-brand-border rounded-xl p-3.5 flex flex-col gap-2.5"
        >
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="px-2 py-0.5 bg-white/[0.06] border border-white/[0.1] rounded font-mono text-xs text-slate-200 font-medium">
                {{ srv.serviceCode || `#${srv.id}` }}
              </span>
              <span class="text-slate-200 text-[13px] flex items-center gap-1">
                <Calendar class="w-3.5 h-3.5 text-brand-green-light" />
                <span>{{ formatDate(srv.serviceDate) }}</span>
              </span>
            </div>
            <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
              {{ srv.statusName || 'Completado' }}
            </span>
          </div>

          <div class="flex flex-col gap-1 text-[13px]">
            <div class="text-slate-200 flex items-center gap-1.5">
              <Car class="w-4 h-4 text-brand-green-light shrink-0" />
              <span>Conductor: <strong class="text-white">{{ srv.driverName || srv.employeeName || selectedDriverLabel }}</strong></span>
            </div>
            <div class="flex items-center justify-between gap-2">
              <div class="text-sm text-white">
                <span class="text-slate-300">Cliente: </span>
                <strong class="font-bold">{{ getClientName(srv) }}</strong>
              </div>
              <button
                type="button"
                class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-white/[0.07] hover:bg-brand-green/20 border border-white/[0.14] hover:border-brand-green/40 text-xs font-semibold text-slate-100 hover:text-brand-green-light transition-all cursor-pointer shrink-0 active:scale-95"
                @click="$emit('edit-service', srv)"
                title="Editar este servicio"
              >
                <Pencil class="w-3.5 h-3.5 text-brand-green-light" />
                <span>Editar</span>
              </button>
            </div>
            <p class="text-[13px] text-slate-200 font-medium">{{ srv.serviceTypeName }}</p>
            <p v-if="srv.description" class="text-[13px] text-slate-300 flex items-start gap-1">
              <MapPin class="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
              <span>{{ srv.description }}</span>
            </p>
            
            <div v-if="srv.notes" class="text-xs text-slate-200 bg-white/[0.06] border border-white/[0.12] rounded-lg px-2.5 py-1.5 mt-1 flex items-center justify-between gap-2 flex-wrap">
              <div class="flex items-center gap-1.5">
                <Receipt class="w-3.5 h-3.5 text-slate-300 shrink-0" />
                <span v-if="parseServiceExpense(srv.notes, srv.id).amount > 0">
                  Gastos: <strong class="text-white">{{ formatMoney(parseServiceExpense(srv.notes, srv.id).amount) }}</strong> ({{ parseServiceExpense(srv.notes, srv.id).detail }})
                </span>
                <span v-else>{{ parseServiceExpense(srv.notes, srv.id).detail }}</span>
              </div>
              <button
                v-if="parseServiceExpense(srv.notes, srv.id).amount > 0"
                type="button"
                @click="onToggleExpenseCash(srv)"
                :class="[
                  'px-2 py-0.5 rounded-full text-[11px] font-bold border transition-all cursor-pointer',
                  parseServiceExpense(srv.notes, srv.id).paidInCash
                    ? 'bg-slate-500/20 border-slate-400/40 text-slate-200'
                    : 'bg-emerald-500/15 border-emerald-500/35 text-emerald-300'
                ]"
              >
                {{ parseServiceExpense(srv.notes, srv.id).paidInCash ? '💵 Pagado en efectivo' : '⏳ Pendiente planilla' }}
              </button>
            </div>
          </div>

          <div class="flex items-center justify-between pt-2 border-t border-brand-border/40 text-[13px]">
            <span class="text-slate-300">Cobrado:</span>
            <span class="text-white font-heading font-bold text-sm">{{ formatMoney(srv.priceCharged) }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.scrollbar-thin::-webkit-scrollbar {
  height: 5px;
}
.scrollbar-thin::-webkit-scrollbar-track {
  background: rgba(0, 0, 0, 0.2);
  border-radius: 4px;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: var(--brand-border-strong);
  border-radius: 4px;
}
.scrollbar-thin::-webkit-scrollbar-thumb:hover {
  background: var(--brand-green);
}
</style>
