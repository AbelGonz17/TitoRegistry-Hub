<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import type { User, Service } from '../types';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

const props = defineProps<{
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

// Estados de filtro
const selectedDriverId = ref<string | number>('all');
const activeFilter = ref<string>('mes'); // 'todos' | 'hoy' | 'semana' | 'mes' | 'mes-anterior' | 'rango'
const customDateFrom = ref<string>('');
const customDateTo = ref<string>('');
const searchQuery = ref<string>('');
const exportingPdf = ref<boolean>(false);

// Lista filtrada de choferes disponibles
const driversList = computed(() => {
  return (props.drivers || []).filter(d => 
    d.role === 'Chofer' || 
    d.role?.includes('Conductor') || 
    (!d.role && !d.isSuperAdmin)
  );
});

// Chofer actualmente seleccionado
const currentDriver = computed(() => {
  if (selectedDriverId.value === 'all') return null;
  return driversList.value.find(d => String(d.id) === String(selectedDriverId.value)) || null;
});

const selectedDriverLabel = computed(() => {
  if (selectedDriverId.value === 'all') return 'Flota Completa';
  return currentDriver.value ? currentDriver.value.fullName : 'Chofer Seleccionado';
});

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

// Total aproximado de viáticos declarados en notas
const totalDietasEstimated = computed(() => {
  return filteredServices.value.reduce((sum, s) => {
    let sub = 0;
    const txt = (s.notes || '').toLowerCase();
    if (txt.includes('desayuno')) sub += 200;
    if (txt.includes('comida')) sub += 300;
    if (txt.includes('cena')) sub += 200;
    const parqueoMatch = txt.match(/parqueo.*?rd\$\s*([\d,]+)/i);
    if (parqueoMatch && parqueoMatch[1]) {
      sub += parseFloat(parqueoMatch[1].replace(/,/g, '')) || 0;
    }
    return sum + sub;
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
    const driverLabel = selectedDriverLabel.value;
    const driverCode = currentDriver.value?.code || (isAll ? 'FLOTA COMPLETA' : 'CHO-001');

    // 1. Membrete Institucional
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(16);
    doc.setTextColor(21, 128, 61); // Verde corporativo Tito
    doc.text('TRANSPORTE TITO', 14, 15);

    doc.setFontSize(8);
    doc.setFont('helvetica', 'italic');
    doc.setTextColor(110, 110, 110);
    doc.text('"Servirle es nuestra prioridad" — Operaciones de Transporte y Logística — Rep. Dom.', 14, 20);

    // 2. Título del Reporte
    doc.setFontSize(13);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(20, 25, 22);
    const title = isAll 
      ? 'INFORME GENERAL DE SERVICIOS — FLOTA COMPLETA' 
      : `INFORME DETALLADO DE SERVICIOS — ${driverLabel.toUpperCase()}`;
    doc.text(title, 14, 28);

    // 3. Metadatos
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

    // 4. Caja Resumen de Métricas
    doc.setFillColor(243, 248, 245);
    doc.setDrawColor(187, 222, 201);
    doc.roundedRect(14, 43, 269, 12, 2, 2, 'FD');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9.5);
    doc.setTextColor(30, 30, 30);
    doc.text(`Total de Servicios: ${filteredServices.value.length} viajes`, 20, 50.5);

    if (totalDietasEstimated.value > 0) {
      doc.setTextColor(180, 83, 9);
      doc.text(`Dietas/Viáticos Reportados: ${formatMoney(totalDietasEstimated.value)}`, 95, 50.5);
    }

    doc.setTextColor(21, 128, 61);
    doc.text(
      `Total Facturado / Cobrado: ${formatMoney(totalBilled.value)}`,
      175,
      50.5
    );

    // 5. Tabla de Servicios con jsPDF AutoTable
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
    <!-- 1. CABECERA PRINCIPAL: Título, Chofer y Botón Sacar Informe -->
    <!-- ======================================================== -->
    <div class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-card/95 via-brand-card to-brand-black/95 border border-brand-border p-5 md:p-6 shadow-card">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div class="flex items-center gap-3.5">
          <div class="w-12 h-12 rounded-2xl bg-brand-green/20 border border-brand-green/40 flex items-center justify-center text-2xl shadow-glow shrink-0">
            <span>📑</span>
          </div>
          <div>
            <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-brand-green/15 text-brand-green-light border border-brand-green/30 uppercase tracking-wider mb-1">
              <span>Supervisión de Flota</span>
              <span class="opacity-50">•</span>
              <span>Reportes Oficiales</span>
            </div>
            <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading leading-tight">
              Servicios por Chofer
            </h1>
            <p class="text-xs text-slate-400 mt-0.5">
              Consulta los viajes registrados por cada conductor, filtra por fechas y genera informes oficiales en PDF.
            </p>
          </div>
        </div>

        <!-- Acciones Principales: Refrescar y Sacar Informe -->
        <div class="flex items-center gap-2.5 flex-wrap">
          <button 
            type="button"
            class="px-3 py-2 bg-brand-charcoal hover:bg-brand-card-hover border border-brand-border rounded-xl text-slate-300 hover:text-white text-xs font-semibold flex items-center gap-1.5 transition-all cursor-pointer shadow-sm"
            @click="$emit('refresh')"
            :disabled="loading"
            title="Refrescar datos"
          >
            <span :class="{ 'animate-spin': loading }">↻</span>
            <span>Actualizar</span>
          </button>

          <button 
            type="button"
            class="inline-flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-emerald-600 to-brand-green hover:from-emerald-500 hover:to-brand-green-light text-white text-xs font-heading font-bold rounded-xl shadow-md hover:shadow-lg transition-all cursor-pointer active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed"
            @click="exportReport"
            :disabled="filteredServices.length === 0 || exportingPdf"
            :title="filteredServices.length === 0 ? 'No hay servicios para exportar' : 'Descargar informe oficial en PDF'"
          >
            <span>{{ exportingPdf ? '⏳' : '📄' }}</span>
            <span>{{ exportingPdf ? 'Generando PDF...' : `Sacar Informe PDF (${selectedDriverLabel})` }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 2. SELECTOR DE CHOFER DESTACADO (CHIPS INTERACTIVOS)     -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-4 shadow-sm">
      <div class="flex items-center justify-between gap-2 mb-3">
        <div class="flex items-center gap-2">
          <span class="text-base">🚐</span>
          <span class="text-xs font-bold text-white uppercase tracking-wider">Selecciona el Conductor a Auditar</span>
        </div>
        <span class="text-xs text-slate-400">{{ driversList.length }} choferes registrados</span>
      </div>

      <!-- Barra de Chips de Choferes -->
      <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-thin">
        <button 
          type="button"
          :class="['px-3 py-1.5 rounded-xl text-xs font-semibold flex items-center gap-2 transition-all cursor-pointer shrink-0 border', selectedDriverId === 'all' ? 'bg-brand-green text-white border-brand-green-light shadow-md shadow-brand-green/20' : 'bg-brand-charcoal/80 text-slate-300 border-brand-border hover:border-brand-green/40 hover:text-white']"
          @click="selectedDriverId = 'all'"
        >
          <span>🌐</span>
          <span>Flota Completa (Todos)</span>
        </button>

        <button 
          v-for="d in driversList"
          :key="d.id"
          type="button"
          :class="['px-3 py-1.5 rounded-xl text-xs font-semibold flex items-center gap-2 transition-all cursor-pointer shrink-0 border', String(selectedDriverId) === String(d.id) ? 'bg-brand-green text-white border-brand-green-light shadow-md shadow-brand-green/20' : 'bg-brand-charcoal/80 text-slate-300 border-brand-border hover:border-brand-green/40 hover:text-white']"
          @click="selectedDriverId = d.id"
        >
          <span>{{ d.avatar || '🚐' }}</span>
          <span>{{ d.fullName }}</span>
          <span class="font-mono text-[10px] opacity-75">({{ d.code || 'CHO' }})</span>
        </button>
      </div>

      <!-- Tarjeta Resumen del Chofer Activo -->
      <div v-if="currentDriver" class="mt-3.5 pt-3.5 border-t border-brand-border/40 flex items-center justify-between flex-wrap gap-3">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-xl bg-brand-green/20 border border-brand-green/30 flex items-center justify-center text-xl shrink-0">
            <span>{{ currentDriver.avatar || '🚐' }}</span>
          </div>
          <div>
            <div class="flex items-center gap-2">
              <strong class="text-white text-sm">{{ currentDriver.fullName }}</strong>
              <span class="px-2 py-0.2 rounded-full font-mono text-[10px] font-bold bg-brand-green/15 text-brand-green-light border border-brand-green/30">
                {{ currentDriver.code || 'CHO-001' }}
              </span>
            </div>
            <p class="text-xs text-slate-400 mt-0.5">
              {{ currentDriver.department || 'Rutas Interurbanas' }} • {{ currentDriver.phone || 'Sin teléfono' }}
            </p>
          </div>
        </div>

        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/20 text-xs font-semibold text-emerald-400">
          <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
          <span>Conductor Activo</span>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 3. TARJETAS DE MÉTRICAS DEL CHOFER SELECCIONADO          -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3">
      <!-- Métrica 1: Viajes Realizados -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-3.5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-400 text-xs mb-1">
          <span class="uppercase font-semibold tracking-wider">Viajes Realizados</span>
          <span class="text-lg">🛣️</span>
        </div>
        <div class="text-2xl font-extrabold text-white font-heading">
          {{ totalServicesCount }}
        </div>
        <div class="text-[11px] text-slate-400 mt-1">
          En {{ periodLabel.toLowerCase() }}
        </div>
      </div>

      <!-- Métrica 2: Total Facturado / Cobrado -->
      <div class="bg-gradient-to-br from-green-950/40 via-brand-card to-brand-card border border-brand-green/40 rounded-xl p-3.5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-300 text-xs mb-1">
          <span class="uppercase font-semibold tracking-wider">Total Facturado</span>
          <span class="text-lg">💵</span>
        </div>
        <div class="text-xl md:text-2xl font-extrabold text-white font-heading">
          {{ formatMoney(totalBilled) }}
        </div>
        <div class="text-[11px] text-brand-green-light mt-1">
          Cobrado a clientes
        </div>
      </div>

      <!-- Métrica 3: Dietas y Viáticos Reportados -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-3.5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-400 text-xs mb-1">
          <span class="uppercase font-semibold tracking-wider">Dietas / Gastos</span>
          <span class="text-lg">🍲</span>
        </div>
        <div class="text-xl md:text-2xl font-extrabold text-amber-300 font-heading">
          {{ formatMoney(totalDietasEstimated) }}
        </div>
        <div class="text-[11px] text-slate-400 mt-1">
          Comidas y parqueos declarados
        </div>
      </div>

      <!-- Métrica 4: Promedio por Viaje -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-3.5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between text-slate-400 text-xs mb-1">
          <span class="uppercase font-semibold tracking-wider">Promedio / Viaje</span>
          <span class="text-lg">📈</span>
        </div>
        <div class="text-xl md:text-2xl font-extrabold text-slate-200 font-heading">
          {{ formatMoney(averagePerService) }}
        </div>
        <div class="text-[11px] text-slate-400 mt-1">
          Ingreso medio por servicio
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 4. BARRA DE FILTROS POR FECHA Y BÚSQUEDA                 -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-3.5 shadow-sm flex items-center justify-between flex-wrap gap-3">
      <!-- Filtros rápidos de tiempo -->
      <div class="flex items-center gap-1.5 flex-wrap">
        <span class="text-xs text-slate-400 font-semibold mr-1">Período:</span>
        <div class="flex bg-brand-black/80 border border-brand-border rounded-lg p-0.5 gap-0.5 flex-wrap">
          <button 
            type="button"
            :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'todos' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'todos'"
          >
            Todos
          </button>
          <button 
            type="button"
            :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'hoy' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'hoy'"
          >
            Hoy
          </button>
          <button 
            type="button"
            :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'semana' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'semana'"
          >
            Semana
          </button>
          <button 
            type="button"
            :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'mes' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'mes'"
          >
            Este Mes
          </button>
          <button 
            type="button"
            :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'mes-anterior' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'mes-anterior'"
          >
            Mes Anterior
          </button>
          <button 
            type="button"
            :class="['px-2.5 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'rango' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'rango'"
          >
            Rango
          </button>
        </div>

        <!-- Inputs de Rango Personalizado -->
        <div v-if="activeFilter === 'rango'" class="flex items-center gap-1.5 bg-brand-charcoal border border-brand-border rounded-lg px-2 py-1 text-xs animate-fadeIn">
          <input 
            v-model="customDateFrom" 
            type="date" 
            class="bg-transparent text-white text-xs outline-none cursor-pointer"
            title="Fecha Inicial"
          />
          <span class="text-slate-500 text-[10px]">a</span>
          <input 
            v-model="customDateTo" 
            type="date" 
            class="bg-transparent text-white text-xs outline-none cursor-pointer"
            title="Fecha Final"
          />
        </div>
      </div>

      <!-- Buscador por texto -->
      <div class="relative flex items-center min-w-[200px] md:min-w-[260px] w-full sm:w-auto">
        <span class="absolute left-2.5 text-xs opacity-60">🔍</span>
        <input 
          v-model="searchQuery" 
          type="text" 
          class="w-full pl-7 pr-6 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-xs placeholder-slate-500 focus:border-brand-green-light outline-none transition-all" 
          placeholder="Buscar cliente, ruta, código..."
        />
        <button v-if="searchQuery" class="absolute right-2 text-slate-400 hover:text-white text-sm cursor-pointer" @click="searchQuery = ''">&times;</button>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 5. TABLA Y LISTADO DETALLADO DE SERVICIOS                -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-4 md:p-5 shadow-card">
      <div class="flex items-center justify-between pb-3 mb-3 border-b border-brand-border/40">
        <div class="flex items-center gap-2">
          <h2 class="text-base font-bold text-white font-heading">
            Detalle de Viajes ({{ filteredServices.length }})
          </h2>
          <span class="text-xs text-slate-400">— {{ selectedDriverLabel }}</span>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="py-12 text-center flex flex-col items-center justify-center gap-2.5">
        <div class="w-7 h-7 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
        <p class="text-xs text-slate-400">Cargando servicios de choferes...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="filteredServices.length === 0" class="py-12 text-center flex flex-col items-center justify-center gap-2 max-w-md mx-auto">
        <div class="text-3xl mb-1">📋</div>
        <h3 class="text-base font-bold text-white">No hay servicios registrados</h3>
        <p class="text-xs text-slate-400">
          No se encontraron viajes para <strong>{{ selectedDriverLabel }}</strong> en el período seleccionado ({{ periodLabel }}).
        </p>
      </div>

      <!-- Tabla para Escritorio (Desktop: md:block) -->
      <div v-else class="hidden md:block overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="border-b border-brand-border/40 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
              <th class="py-2.5 px-3">Código</th>
              <th class="py-2.5 px-3">Chofer</th>
              <th class="py-2.5 px-3">Fecha</th>
              <th class="py-2.5 px-3">Cliente</th>
              <th class="py-2.5 px-3">Ruta / Servicio</th>
              <th class="py-2.5 px-3">Detalle / Ruta</th>
              <th class="py-2.5 px-3">Dietas / Observaciones</th>
              <th class="py-2.5 px-3">Estado</th>
              <th class="py-2.5 px-3 text-right">Cobrado</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-brand-border/20 text-xs">
            <tr v-for="srv in filteredServices" :key="srv.id" class="hover:bg-white/[0.02] transition-colors">
              <td class="py-2.5 px-3">
                <span class="inline-block px-1.5 py-0.5 bg-white/[0.04] border border-white/[0.08] rounded font-mono text-[10px] text-slate-400 font-medium">
                  {{ srv.serviceCode || `#${srv.id}` }}
                </span>
              </td>
              <td class="py-2.5 px-3">
                <div class="flex items-center gap-1.5">
                  <span class="text-xs">🚐</span>
                  <span class="font-semibold text-white">{{ srv.driverName || srv.employeeName || selectedDriverLabel }}</span>
                </div>
              </td>
              <td class="py-2.5 px-3 text-slate-400 whitespace-nowrap">{{ formatDate(srv.serviceDate) }}</td>
              <td class="py-2.5 px-3">
                <div class="font-semibold text-white">{{ getClientName(srv) }}</div>
                <div v-if="srv.clientPhone" class="text-[11px] text-slate-400">{{ srv.clientPhone }}</div>
              </td>
              <td class="py-2.5 px-3">
                <div class="font-medium text-slate-200">{{ srv.serviceTypeName }}</div>
              </td>
              <td class="py-2.5 px-3">
                <div class="text-slate-300 truncate max-w-xs" :title="srv.description || srv.clientAddress">
                  {{ srv.description || srv.clientAddress || '-' }}
                </div>
              </td>
              <td class="py-2.5 px-3">
                <div v-if="srv.notes" class="inline-block text-[11px] text-amber-300/90 bg-amber-500/10 border border-amber-500/20 rounded-md px-2 py-0.5 max-w-xs truncate" :title="srv.notes">
                  {{ srv.notes }}
                </div>
                <span v-else class="text-[10px] text-slate-500 italic">-</span>
              </td>
              <td class="py-2.5 px-3">
                <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                  {{ srv.statusName || 'Completado' }}
                </span>
              </td>
              <td class="py-2.5 px-3 text-right font-heading font-semibold text-slate-200 whitespace-nowrap">
                {{ formatMoney(srv.priceCharged) }}
              </td>
            </tr>
          </tbody>
          <!-- Pie de Tabla con Totales -->
          <tfoot>
            <tr class="border-t border-brand-border/60 bg-white/[0.02] text-xs font-bold text-white">
              <td colspan="8" class="py-3 px-3 text-right uppercase tracking-wider text-slate-400 font-heading">
                Total Facturado ({{ filteredServices.length }} viajes):
              </td>
              <td class="py-3 px-3 text-right text-brand-green-light font-heading text-sm font-extrabold whitespace-nowrap">
                {{ formatMoney(totalBilled) }}
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      <!-- Vista de Tarjetas para Móviles (Mobile: md:hidden) -->
      <div v-if="!loading && filteredServices.length > 0" class="md:hidden flex flex-col gap-2.5">
        <div 
          v-for="srv in filteredServices" 
          :key="srv.id" 
          class="bg-brand-charcoal/80 border border-brand-border rounded-xl p-3 flex flex-col gap-2"
        >
          <!-- Fila Superior -->
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="inline-block px-1.5 py-0.5 bg-white/[0.04] border border-white/[0.08] rounded font-mono text-[10px] text-slate-400 font-medium">
                {{ srv.serviceCode || `#${srv.id}` }}
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

          <!-- Datos de Chofer y Cliente -->
          <div class="flex flex-col gap-0.5">
            <div class="text-xs text-yellow-300/90 font-semibold flex items-center gap-1">
              <span>🚐 Conductor:</span>
              <strong class="text-white">{{ srv.driverName || srv.employeeName || selectedDriverLabel }}</strong>
            </div>
            <h3 class="text-xs font-bold text-white">{{ getClientName(srv) }}</h3>
            <p class="text-[11px] text-slate-300">🚐 {{ srv.serviceTypeName }}</p>
            <p v-if="srv.description" class="text-[11px] text-slate-400 truncate">📍 {{ srv.description }}</p>
            
            <!-- Dietas y Observaciones -->
            <div v-if="srv.notes" class="text-[11px] text-amber-300/90 bg-amber-500/10 border border-amber-500/20 rounded-lg px-2 py-1 mt-1">
              <span class="font-semibold text-amber-200">📝 Notas / Dietas:</span> {{ srv.notes }}
            </div>
          </div>

          <!-- Monto Cobrado -->
          <div class="flex items-center justify-between pt-2 border-t border-brand-border/30 text-xs">
            <span class="text-slate-400">Cobrado:</span>
            <span class="text-slate-100 font-heading font-bold text-sm">{{ formatMoney(srv.priceCharged) }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.scrollbar-thin::-webkit-scrollbar {
  height: 6px;
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
