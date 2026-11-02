<script setup lang="ts">
import { ref, computed } from 'vue';
import type { User, Service, EmployeeSummary } from '../types';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

const props = defineProps<{
  employeeSummary?: EmployeeSummary;
  services?: Service[];
  loading?: boolean;
  currentUser?: User | null;
  drivers?: User[];
}>();

const emit = defineEmits<{
  (e: 'open-new-service'): void;
  (e: 'refresh'): void;
  (e: 'notify', message: string, type?: string): void;
}>();

const searchQuery = ref('');
const activeFilter = ref('todos'); // 'todos' | 'hoy' | 'semana' | 'mes' | 'rango'
const selectedDriverId = ref('all'); // 'all' | id del chofer
const customDateFrom = ref('');
const customDateTo = ref('');
const exportingPdf = ref(false);

// Determinar si el usuario autenticado es Administrador / SuperAdmin
const isAdmin = computed(() => {
  return props.currentUser?.isSuperAdmin || 
         props.currentUser?.role === 'Administrador' || 
         props.currentUser?.role === 'SuperAdmin';
});

// Lista de choferes para el selector del administrador
const driversList = computed(() => {
  return (props.drivers || []).filter(d => 
    d.role === 'Chofer' || 
    d.role?.includes('Conductor') || 
    (!d.role && !d.isSuperAdmin)
  );
});

// Filtrado de servicios según período y búsqueda
const filteredServices = computed(() => {
  let list = props.services || [];

  // Filtro por chofer (solo para administradores)
  if (isAdmin.value && selectedDriverId.value !== 'all') {
    const targetId = Number(selectedDriverId.value);
    list = list.filter(s => (s.driverId === targetId || s.employeeId === targetId));
  }

  // Filtro de tiempo
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
  } else if (activeFilter.value === 'rango') {
    if (customDateFrom.value) {
      list = list.filter(s => (s.serviceDate || '').slice(0, 10) >= customDateFrom.value);
    }
    if (customDateTo.value) {
      list = list.filter(s => (s.serviceDate || '').slice(0, 10) <= customDateTo.value);
    }
  }

  // Filtro de búsqueda por cliente, ruta, chofer o código
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(s => 
      s.clientName?.toLowerCase().includes(q) ||
      s.clientReference?.toLowerCase().includes(q) ||
      s.clientAddress?.toLowerCase().includes(q) ||
      s.description?.toLowerCase().includes(q) ||
      s.serviceTypeName?.toLowerCase().includes(q) ||
      s.serviceCode?.toLowerCase().includes(q) ||
      s.driverName?.toLowerCase().includes(q) ||
      s.employeeName?.toLowerCase().includes(q)
    );
  }

  return list;
});

// Métricas dinámicas calculadas (usadas por el administrador)
const totalCompletedServices = computed(() => {
  return filteredServices.value.length;
});

const totalBilledServices = computed(() => {
  return filteredServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0);
});

const getDriver = (driverId) => {
  return (props.drivers || []).find(d => d.id === driverId);
};

const getServiceEarnings = (srv) => {
  if (srv.employeeEarnings !== undefined && srv.employeeEarnings !== null) {
    return Number(srv.employeeEarnings);
  }
  const driver = getDriver(srv.driverId || srv.employeeId);
  const rate = driver?.commissionRate ?? (props.employeeSummary?.commissionRate || 25);
  return (Number(srv.priceCharged) || 0) * (rate / 100);
};

const selectedDriverLabel = computed(() => {
  if (selectedDriverId.value === 'all') return 'Flota Completa';
  const found = driversList.value.find(d => d.id === Number(selectedDriverId.value));
  return found ? found.fullName : 'Chofer Seleccionado';
});

const formatTripCount = (count) => {
  return count === 1 ? '1 viaje' : `${count} viajes`;
};

const formatMoney = (val, forceSign = false) => {
  const num = Number(val) || 0;
  const absFormatted = Math.abs(num).toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
  if (num < 0) {
    return `-RD$ ${absFormatted}`;
  }
  if (forceSign && num > 0) {
    return `+RD$ ${absFormatted}`;
  }
  return `RD$ ${absFormatted}`;
};

const getClientName = (srv) => {
  return srv.clientName || srv.clientReference || 'Cliente Corporativo';
};

const getServiceRoute = (srv) => {
  return srv.clientAddress || srv.description || '';
};

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
};

// Generar informe oficial de servicios en PDF por chofer o flota completa
const exportDriverServicesReport = () => {
  if (filteredServices.value.length === 0) return;

  try {
    exportingPdf.value = true;
    const doc = new jsPDF({
      orientation: 'landscape',
      unit: 'mm',
      format: 'a4'
    });

    const isAllDrivers = selectedDriverId.value === 'all';
    const driverLabel = selectedDriverLabel.value;
    const selectedDriverObj = driversList.value.find(d => d.id === Number(selectedDriverId.value));
    const driverCode = selectedDriverObj?.code || (isAllDrivers ? 'FLOTA COMPLETA' : 'CHO');

    let periodText = 'Histórico Completo';
    if (activeFilter.value === 'hoy') periodText = 'Hoy';
    else if (activeFilter.value === 'semana') periodText = 'Últimos 7 días';
    else if (activeFilter.value === 'mes') periodText = 'Este Mes';
    else if (activeFilter.value === 'rango') {
      periodText = `Del ${customDateFrom.value || 'inicio'} al ${customDateTo.value || 'hoy'}`;
    }

    // Encabezado institucional
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(16);
    doc.setTextColor(21, 128, 61); // Verde institucional
    doc.text('TRANSPORTE TITO', 14, 15);

    doc.setFontSize(8);
    doc.setFont('helvetica', 'italic');
    doc.setTextColor(110, 110, 110);
    doc.text('"Servirle es nuestra prioridad" — R.D.', 14, 20);

    // Título del reporte
    doc.setFontSize(13);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(20, 20, 20);
    const title = isAllDrivers 
      ? 'INFORME DE SERVICIOS — FLOTA COMPLETA' 
      : `INFORME DE SERVICIOS REALIZADOS — ${driverLabel.toUpperCase()}`;
    doc.text(title, 14, 28);

    // Metadatos del informe
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(70, 70, 70);
    doc.text(`Conductor: ${driverLabel} (${driverCode})`, 14, 34);
    doc.text(`Período Consultado: ${periodText}`, 14, 39);

    const nowStr = new Date().toLocaleString('es-DO', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
    doc.text(`Fecha de Emisión: ${nowStr}`, 175, 34);
    doc.text(`Generado por: ${props.currentUser?.fullName || 'Administración'}`, 175, 39);

    // Caja resumen
    doc.setFillColor(243, 248, 245);
    doc.setDrawColor(187, 222, 201);
    doc.roundedRect(14, 43, 269, 12, 2, 2, 'FD');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9.5);
    doc.setTextColor(30, 30, 30);
    doc.text(`Total de Servicios: ${filteredServices.value.length} viajes`, 20, 50.5);

    doc.setTextColor(21, 128, 61);
    doc.text(
      `Total Facturado / Cobrado: RD$ ${totalBilledServices.value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`,
      175,
      50.5
    );

    // Datos de la tabla
    const tableData = filteredServices.value.map((s, index) => [
      index + 1,
      s.serviceCode || `#${s.id}`,
      formatDate(s.serviceDate),
      s.driverName || s.employeeName || driverLabel,
      getClientName(s),
      s.serviceTypeName || 'Servicio',
      s.description || '-',
      s.notes || '-',
      `RD$ ${(Number(s.priceCharged) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
    ]);

    autoTable(doc, {
      startY: 59,
      head: [[
        '#',
        'CÓDIGO',
        'FECHA',
        'CHOFER',
        'CLIENTE / EMPRESA',
        'RUTA / SERVICIO',
        'DETALLE DE SERVICIO',
        'DIETAS / OBSERVACIONES',
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
        { content: `RD$ ${totalBilledServices.value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, styles: { halign: 'right', fontStyle: 'bold', fontSize: 8.5, textColor: [21, 128, 61] } }
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
    emit('notify', 'Hubo un inconveniente al generar el informe en PDF.', 'error');
  } finally {
    exportingPdf.value = false;
  }
};
</script>

<template>
  <div class="flex flex-col gap-4 md:gap-5">
    <!-- ======================================================== -->
    <!-- 1. HERO / CABECERA COMPACTA ADAPTATIVA SEGÚN ROL         -->
    <!-- ======================================================== -->

    <!-- Cabecera para el ADMINISTRADOR -->
    <div 
      v-if="isAdmin" 
      class="relative overflow-hidden rounded-xl bg-gradient-to-br from-brand-card/95 to-brand-black/98 border border-white/[0.08] p-4 md:p-5 shadow-sm flex items-center justify-between flex-wrap gap-4 before:absolute before:top-0 before:left-0 before:w-1.5 before:h-full before:bg-gradient-to-b before:from-amber-400 before:to-amber-600"
    >
      <div class="flex items-center gap-3.5">
        <div class="w-12 h-12 rounded-xl bg-amber-500/10 border border-amber-500/30 flex items-center justify-center shadow-md shadow-amber-500/10 text-2xl shrink-0">
          <span>{{ currentUser?.avatar || '👑' }}</span>
        </div>

        <div class="flex flex-col">
          <div class="flex items-center gap-2.5 flex-wrap">
            <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading tracking-tight leading-tight">
              {{ currentUser?.fullName || 'Tito González' }}
            </h1>
            <div class="flex items-center gap-1.5">
              <span class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-white/[0.06] border border-white/[0.12] text-slate-200">
                {{ currentUser?.isSuperAdmin ? 'Propietario' : 'Administrador' }}
              </span>
              <span v-if="currentUser?.isSuperAdmin" class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-white/[0.06] border border-white/[0.12] text-slate-300">
                SuperAdmin
              </span>
            </div>
          </div>

          <div class="flex items-center gap-2 mt-1 text-xs text-slate-400 font-medium">
            <span class="font-mono px-1.5 py-0.2 rounded bg-white/[0.05] border border-white/[0.08] text-slate-300 text-[10px]">
              {{ currentUser?.code || 'ADM-001' }}
            </span>
            <span class="opacity-40">•</span>
            <span class="truncate">Supervisión y Control de Flota</span>
          </div>
        </div>
      </div>

      <div class="flex items-center shrink-0">
        <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-white/[0.04] border border-white/[0.1] text-xs font-medium text-slate-300 select-none">
          <span class="relative flex h-2 w-2">
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
          </span>
          <span>Supervisión Activa</span>
        </div>
      </div>
    </div>

    <!-- Cabecera para el CHOFER (Limpia y Compacta) -->
    <div 
      v-else 
      class="relative overflow-hidden rounded-xl bg-gradient-to-br from-brand-card/95 to-brand-black/98 border border-brand-green/30 p-4 md:p-5 shadow-sm flex items-center justify-between flex-wrap gap-4 before:absolute before:top-0 before:left-0 before:w-1.5 before:h-full before:bg-gradient-to-b before:from-brand-green-light before:to-brand-green"
    >
      <div class="flex items-center gap-3.5">
        <div class="w-12 h-12 rounded-xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center shadow-md shadow-brand-green/10 text-2xl shrink-0">
          <span>🚐</span>
        </div>
        <div>
          <div class="inline-flex items-center gap-2 px-2 py-0.5 bg-brand-green/15 border border-brand-border rounded-full text-[10px] font-bold text-brand-green-light mb-1 uppercase tracking-wider">
            <span>{{ employeeSummary?.code || currentUser?.code || 'CHO-001' }}</span>
            <span class="opacity-50">•</span>
            <span>Comisión: {{ employeeSummary?.commissionRate || currentUser?.commissionRate || 25 }}%</span>
          </div>
          <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading leading-tight">
            {{ employeeSummary?.fullName || currentUser?.fullName || 'Chofer Activo' }}
          </h1>
          <p class="text-slate-400 text-xs mt-0.5">{{ employeeSummary?.role || currentUser?.role || 'Conductor' }} — {{ employeeSummary?.department || currentUser?.department || 'Operaciones' }}</p>
        </div>
      </div>

      <div class="flex items-center shrink-0">
        <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-brand-green/10 border border-brand-green/25 text-xs font-medium text-brand-green-light select-none">
          <span class="relative flex h-2 w-2">
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
          </span>
          <span>Conductor Activo</span>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 2. RESUMEN DE FLOTA (EXCLUSIVO PARA EL ADMINISTRADOR)    -->
    <!-- ======================================================== -->
    <div v-if="isAdmin" class="grid grid-cols-1 sm:grid-cols-2 gap-3">
      <!-- Tarjeta 1: Viajes Realizados por Choferes -->
      <div class="bg-brand-card border border-brand-border hover:border-brand-border-strong rounded-xl p-3.5 md:p-4 shadow-sm flex items-center justify-between gap-3 transition-all">
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-10 h-10 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-lg shrink-0">
            <span>🛣️</span>
          </div>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Viajes Completados</span>
            <span class="text-xs text-slate-400 truncate">{{ formatTripCount(totalCompletedServices) }} · {{ selectedDriverLabel }}</span>
          </div>
        </div>
        <div class="text-right shrink-0">
          <span class="text-2xl md:text-3xl font-extrabold font-heading text-white">{{ totalCompletedServices }}</span>
        </div>
      </div>

      <!-- Tarjeta 2: Total Facturado Flota -->
      <div class="bg-gradient-to-br from-green-950/40 to-brand-card border border-brand-green/30 hover:border-brand-green-light/40 rounded-xl p-3.5 md:p-4 shadow-sm flex items-center justify-between gap-3 transition-all">
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-10 h-10 rounded-xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-lg shrink-0">
            <span>💵</span>
          </div>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Total Facturado</span>
            <span class="text-xs text-brand-green-light font-medium">Volumen de flota</span>
          </div>
        </div>
        <div class="text-right shrink-0">
          <span class="text-xl md:text-2xl font-extrabold font-heading text-white tracking-tight">
            {{ formatMoney(totalBilledServices) }}
          </span>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 3. SECCIÓN DE HISTORIAL DE VIAJES REGISTRADOS            -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-4 md:p-5 shadow-card">
      <div class="flex items-center justify-between flex-wrap gap-3 mb-4 pb-3 border-b border-brand-border/40">
        <!-- Título y Contador de Viajes + Botones de Acción -->
        <div class="flex items-center gap-2.5 flex-wrap">
          <h2 class="text-base md:text-lg font-extrabold text-white font-heading tracking-tight">
            {{ isAdmin ? 'Servicios de la Flota' : 'Historial de Viajes' }}
          </h2>
          <span class="bg-brand-green/15 text-brand-green-light px-2.5 py-0.5 rounded-full text-xs font-bold border border-brand-green/20">
            {{ formatTripCount(filteredServices.length) }}
          </span>

          <!-- Botón Sacar Informe PDF para el Administrador -->
          <button 
            v-if="isAdmin"
            type="button"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-emerald-600 to-brand-green hover:from-emerald-500 hover:to-brand-green-light text-white text-xs font-heading font-bold rounded-xl shadow-md hover:shadow-lg transition-all cursor-pointer active:scale-95 shrink-0 disabled:opacity-50 disabled:cursor-not-allowed"
            @click="exportDriverServicesReport"
            :disabled="filteredServices.length === 0 || exportingPdf"
            :title="filteredServices.length === 0 ? 'No hay servicios para exportar' : 'Descargar informe oficial en PDF'"
          >
            <span>{{ exportingPdf ? '⏳' : '📄' }}</span>
            <span>{{ exportingPdf ? 'Generando...' : `Sacar Informe PDF (${selectedDriverLabel})` }}</span>
          </button>

          <button 
            v-if="!isAdmin"
            type="button"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white text-xs font-heading font-semibold rounded-xl shadow-sm hover:shadow-md transition-all cursor-pointer active:scale-95 shrink-0"
            @click="$emit('open-new-service')"
          >
            <span class="text-sm font-bold leading-none">+</span>
            <span>Registrar Viaje</span>
          </button>
        </div>

        <!-- Filtros de Período y Búsqueda -->
        <div class="flex items-center gap-2.5 flex-wrap">
          <!-- Filtro por Chofer (Exclusivo Administrador) -->
          <div v-if="isAdmin" class="flex items-center gap-1.5">
            <label class="text-[11px] font-bold text-slate-400 uppercase tracking-wider" for="driver-filter">Chofer:</label>
            <div class="relative flex items-center">
              <span class="absolute left-2.5 text-xs pointer-events-none">🚐</span>
              <select 
                id="driver-filter" 
                v-model="selectedDriverId" 
                class="pl-7 pr-3 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-xs font-semibold focus:border-brand-green-light focus:ring-1 focus:ring-brand-green-light outline-none cursor-pointer transition-all"
              >
                <option value="all">Todos (Flota Completa)</option>
                <option v-for="d in driversList" :key="d.id" :value="d.id">
                  {{ d.fullName }} ({{ d.code || 'CHO' }})
                </option>
              </select>
            </div>
          </div>

          <!-- Filtro rápido de tiempo -->
          <div class="flex bg-brand-black/80 border border-brand-border rounded-lg p-0.5 gap-0.5">
            <button 
              :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'todos' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
              @click="activeFilter = 'todos'"
            >
              Todos
            </button>
            <button 
              :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'hoy' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
              @click="activeFilter = 'hoy'"
            >
              Hoy
            </button>
            <button 
              :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'semana' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
              @click="activeFilter = 'semana'"
            >
              Semana
            </button>
            <button 
              :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'mes' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
              @click="activeFilter = 'mes'"
            >
              Mes
            </button>
            <button 
              :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'rango' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
              @click="activeFilter = 'rango'"
            >
              Rango
            </button>
          </div>

          <!-- Rango de fechas condicional -->
          <div v-if="activeFilter === 'rango'" class="flex items-center gap-1 bg-brand-charcoal border border-brand-border rounded-lg px-2 py-1 text-xs">
            <input 
              v-model="customDateFrom" 
              type="date" 
              class="bg-transparent text-white text-xs outline-none cursor-pointer"
              title="Fecha inicial"
            />
            <span class="text-slate-500 text-[10px]">a</span>
            <input 
              v-model="customDateTo" 
              type="date" 
              class="bg-transparent text-white text-xs outline-none cursor-pointer"
              title="Fecha final"
            />
          </div>

          <!-- Campo de búsqueda por cliente o ruta -->
          <div class="relative flex items-center min-w-[170px] md:min-w-[210px]">
            <span class="absolute left-2.5 text-xs opacity-60">🔍</span>
            <input 
              v-model="searchQuery" 
              type="text" 
              class="w-full pl-7 pr-6 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-xs placeholder-slate-500 focus:border-brand-green-light focus:ring-1 focus:ring-brand-green-light outline-none transition-all" 
              :placeholder="isAdmin ? 'Buscar cliente, ruta o chofer...' : 'Buscar cliente o ruta...'"
            />
            <button v-if="searchQuery" class="absolute right-2 text-slate-400 hover:text-white text-sm cursor-pointer" @click="searchQuery = ''">&times;</button>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="py-12 text-center flex flex-col items-center justify-center gap-2.5">
        <div class="w-7 h-7 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
        <p class="text-xs text-slate-400">Cargando información de viajes...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="filteredServices.length === 0" class="py-12 text-center flex flex-col items-center justify-center gap-2.5 max-w-md mx-auto">
        <div class="text-3xl mb-1">{{ isAdmin ? '📋' : '🚐' }}</div>
        <h3 class="text-base font-bold text-white">No se encontraron viajes</h3>
        <p v-if="isAdmin" class="text-xs text-slate-400">
          {{ selectedDriverId !== 'all' ? `El chofer ${selectedDriverLabel} no tiene viajes en este período.` : 'No hay servicios registrados en el período seleccionado.' }}
        </p>
        <p v-else class="text-xs text-slate-400">
          No tienes viajes registrados para el período seleccionado. ¡Registra tu próximo viaje para comenzar a acumular!
        </p>
        <button 
          v-if="!isAdmin"
          type="button"
          class="mt-1 px-4 py-2 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white text-xs font-semibold rounded-xl shadow-md transition-all cursor-pointer"
          @click="$emit('open-new-service')"
        >
          + Registrar Nuevo Viaje
        </button>
      </div>

      <!-- Tabla para Escritorio (Desktop: md:block) -->
      <div v-else class="hidden md:block overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="border-b border-brand-border/40 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
              <th class="py-2.5 px-3">Código</th>
              <th v-if="isAdmin" class="py-2.5 px-3">Chofer</th>
              <th class="py-2.5 px-3">Fecha</th>
              <th class="py-2.5 px-3">Cliente</th>
              <th class="py-2.5 px-3">Ruta / Servicio</th>
              <th class="py-2.5 px-3">Dietas / Observaciones</th>
              <th class="py-2.5 px-3">Estado</th>
              <th class="py-2.5 px-3 text-right">Cobrado</th>
              <!-- Oculto temporalmente: Comisión -->
              <!-- <th class="py-2.5 px-3 text-right">{{ isAdmin ? 'Comisión Chofer' : 'Tu Comisión' }}</th> -->
            </tr>
          </thead>
          <tbody class="divide-y divide-brand-border/20 text-xs">
            <tr v-for="srv in filteredServices" :key="srv.id" class="hover:bg-white/[0.02] transition-colors">
              <td class="py-2.5 px-3">
                <span class="inline-block px-1.5 py-0.5 bg-white/[0.04] border border-white/[0.08] rounded font-mono text-[10px] text-slate-400 font-medium">
                  {{ srv.serviceCode }}
                </span>
              </td>
              <td v-if="isAdmin" class="py-2.5 px-3">
                <div class="flex items-center gap-1.5">
                  <span class="text-xs">🚐</span>
                  <span class="font-semibold text-white">{{ srv.driverName || srv.employeeName || 'Chofer' }}</span>
                </div>
              </td>
              <td class="py-2.5 px-3 text-slate-400">{{ formatDate(srv.serviceDate) }}</td>
              <td class="py-2.5 px-3">
                <div class="font-semibold text-white">{{ getClientName(srv) }}</div>
                <div v-if="srv.clientPhone" class="text-[11px] text-slate-400">{{ srv.clientPhone }}</div>
              </td>
              <td class="py-2.5 px-3">
                <div class="font-medium text-slate-200">{{ srv.serviceTypeName }}</div>
                <div v-if="getServiceRoute(srv)" class="text-[11px] text-slate-400 truncate max-w-xs">📍 {{ getServiceRoute(srv) }}</div>
              </td>
              <td class="py-2.5 px-3">
                <span v-if="srv.notes" class="inline-block text-[11px] text-amber-300/90 bg-amber-500/10 border border-amber-500/20 rounded-md px-2 py-0.5 max-w-xs truncate" :title="srv.notes">
                  {{ srv.notes }}
                </span>
                <span v-else class="text-[10px] text-slate-500 italic">-</span>
              </td>
              <td class="py-2.5 px-3">
                <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                  {{ srv.statusName || 'Completado' }}
                </span>
              </td>
              <td class="py-2.5 px-3 text-right font-heading font-semibold text-slate-200">
                {{ formatMoney(srv.priceCharged) }}
              </td>
              <!-- Oculto temporalmente: Comisión -->
              <!--
              <td class="py-2.5 px-3 text-right font-heading font-bold text-brand-green-light">
                {{ formatMoney(getServiceEarnings(srv), true) }}
              </td>
              -->
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Vista de Tarjetas para Móviles (Mobile: md:hidden) -->
      <div v-if="!loading && filteredServices.length > 0" class="md:hidden flex flex-col gap-2.5">
        <div 
          v-for="srv in filteredServices" 
          :key="srv.id" 
          class="bg-brand-charcoal/80 border border-brand-border rounded-xl p-3 flex flex-col gap-2"
        >
          <!-- Fila Superior: Código Discreto + Fecha + Estado -->
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="inline-block px-1.5 py-0.5 bg-white/[0.04] border border-white/[0.08] rounded font-mono text-[10px] text-slate-400 font-medium">
                {{ srv.serviceCode }}
              </span>
              <span class="text-slate-400 text-xs flex items-center gap-1">
                <span>📅</span>
                <span>{{ formatDate(srv.serviceDate) }}</span>
              </span>
            </div>
            <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
              {{ srv.statusName || 'Completado' }}
            </span>
          </div>

          <!-- Contenido Central: Cliente y Ruta -->
          <div class="flex flex-col gap-0.5">
            <div v-if="isAdmin" class="text-xs text-yellow-300/90 font-semibold flex items-center gap-1">
              <span>🚐 Conductor:</span>
              <strong class="text-white">{{ srv.driverName || srv.employeeName || 'Chofer' }}</strong>
            </div>
            <h3 class="text-xs font-bold text-white">{{ getClientName(srv) }}</h3>
            <p class="text-[11px] text-slate-300">🚐 {{ srv.serviceTypeName }}</p>
            <p v-if="getServiceRoute(srv)" class="text-[11px] text-slate-400 truncate">📍 {{ getServiceRoute(srv) }}</p>
            <!-- Dietas y Observaciones en Móvil -->
            <div v-if="srv.notes" class="text-[11px] text-amber-300/90 bg-amber-500/10 border border-amber-500/20 rounded-lg px-2 py-1 mt-1">
              <span class="font-semibold text-amber-200">📝 Notas / Dietas:</span> {{ srv.notes }}
            </div>
          </div>

          <!-- Fila Inferior con Etiquetas Explícitas de Montos -->
          <div class="flex items-center justify-between pt-2 border-t border-brand-border/30 text-[11px]">
            <div class="flex items-center gap-1">
              <span class="text-slate-400">Cobrado:</span>
              <span class="text-slate-200 font-heading font-semibold">{{ formatMoney(srv.priceCharged) }}</span>
            </div>
            <!-- Oculto temporalmente: Tu comisión -->
            <!--
            <div class="flex items-center gap-1">
              <span class="text-slate-400">Tu comisión:</span>
              <span class="text-brand-green-light font-heading font-bold">{{ formatMoney(getServiceEarnings(srv), true) }}</span>
            </div>
            -->
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
