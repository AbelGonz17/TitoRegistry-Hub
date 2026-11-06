<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import type { User, Service, EmployeeSummary } from '../types';
import { parseServiceExpense } from '../utils/expenses';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import {
  Car,
  ShieldCheck,
  Wallet,
  Banknote,
  CalendarCheck,
  TrendingUp,
  Plus,
  ArrowRight,
  FileDown,
  Search,
  Calendar,
  MapPin,
  Receipt,
  FileText,
  Pencil,
  X
} from 'lucide-vue-next';

const props = defineProps<{
  employeeSummary?: EmployeeSummary;
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

const router = useRouter();

const searchQuery = ref('');
const activeFilter = ref('todos'); // Usado en modo Administrador
const selectedDriverId = ref('all'); // 'all' | id del chofer (Admin)
const customDateFrom = ref('');
const customDateTo = ref('');
const exportingPdf = ref(false);

// Determinar si el usuario autenticado es Administrador / SuperAdmin
const isAdmin = computed(() => {
  return props.currentUser?.isSuperAdmin || 
         props.currentUser?.role === 'Administrador' || 
         props.currentUser?.role === 'SuperAdmin';
});

const driverCommissionRate = computed(() => {
  return props.employeeSummary?.commissionRate || props.currentUser?.commissionRate || 25;
});

// Lista de choferes para el selector del administrador
const driversList = computed(() => {
  return (props.drivers || []).filter(d => 
    d.role === 'Chofer' || 
    d.role?.includes('Conductor') || 
    (!d.role && !d.isSuperAdmin)
  );
});

const getDriver = (driverId?: number | string) => {
  return (props.drivers || []).find(d => d.id === driverId);
};

const getServiceEarnings = (srv: Service) => {
  if (srv.employeeEarnings !== undefined && srv.employeeEarnings !== null) {
    return Number(srv.employeeEarnings);
  }
  const driver = getDriver(srv.driverId || srv.employeeId);
  const rate = driver?.commissionRate ?? driverCommissionRate.value;
  return (Number(srv.priceCharged) || 0) * (rate / 100);
};

// =========================================================================
// MÉTRICAS DE RESUMEN PARA EL PANEL DEL CHOFER (Mes, Hoy, Semana, Recientes)
// =========================================================================
const driverAllServices = computed(() => props.services || []);

const todayServices = computed(() => {
  const todayStr = new Date().toISOString().slice(0, 10);
  return driverAllServices.value.filter(s => s.serviceDate?.slice(0, 10) === todayStr);
});

const weekServices = computed(() => {
  const now = new Date();
  const weekAgo = new Date();
  weekAgo.setDate(now.getDate() - 7);
  return driverAllServices.value.filter(s => new Date(s.serviceDate) >= weekAgo);
});

const monthServices = computed(() => {
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth();
  const inCurrentCalendarMonth = driverAllServices.value.filter(s => {
    if (!s.serviceDate) return false;
    const d = new Date(s.serviceDate);
    return d.getFullYear() === currentYear && d.getMonth() === currentMonth;
  });
  // Si no hay del mes calendario actual, usar los últimos 30 días para que el resumen siempre sea útil
  if (inCurrentCalendarMonth.length > 0) return inCurrentCalendarMonth;
  const monthAgo = new Date();
  monthAgo.setMonth(now.getMonth() - 1);
  return driverAllServices.value.filter(s => new Date(s.serviceDate) >= monthAgo);
});

const monthTotalBilled = computed(() =>
  monthServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0)
);

const monthTotalCommission = computed(() =>
  monthServices.value.reduce((sum, s) => sum + getServiceEarnings(s), 0)
);

const todayTotalBilled = computed(() =>
  todayServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0)
);

const todayTotalCommission = computed(() =>
  todayServices.value.reduce((sum, s) => sum + getServiceEarnings(s), 0)
);

const weekTotalBilled = computed(() =>
  weekServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0)
);

const weekTotalCommission = computed(() =>
  weekServices.value.reduce((sum, s) => sum + getServiceEarnings(s), 0)
);

const recentDriverServices = computed(() => {
  return [...driverAllServices.value]
    .sort((a, b) => new Date(b.serviceDate).getTime() - new Date(a.serviceDate).getTime())
    .slice(0, 5);
});

// =========================================================================
// FILTRADO DE SERVICIOS PARA EL PANEL DEL ADMINISTRADOR
// =========================================================================
const filteredServices = computed(() => {
  let list = props.services || [];

  if (isAdmin.value && selectedDriverId.value !== 'all') {
    const targetId = Number(selectedDriverId.value);
    list = list.filter(s => (s.driverId === targetId || s.employeeId === targetId));
  }

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

const totalCompletedServices = computed(() => filteredServices.value.length);
const totalBilledServices = computed(() =>
  filteredServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0)
);
const totalAdminCommissions = computed(() =>
  filteredServices.value.reduce((sum, s) => sum + getServiceEarnings(s), 0)
);

const selectedDriverLabel = computed(() => {
  if (selectedDriverId.value === 'all') return 'Flota Completa';
  const found = driversList.value.find(d => d.id === Number(selectedDriverId.value));
  return found ? found.fullName : 'Chofer Seleccionado';
});

const formatTripCount = (count: number) => {
  return count === 1 ? '1 viaje' : `${count} viajes`;
};

const formatMoney = (val: number | string | undefined, forceSign = false) => {
  const num = Number(val) || 0;
  const absFormatted = Math.abs(num).toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
  if (num < 0) return `-RD$ ${absFormatted}`;
  if (forceSign && num > 0) return `+RD$ ${absFormatted}`;
  return `RD$ ${absFormatted}`;
};

const getClientName = (srv: Service) => {
  return srv.clientName || srv.clientReference || 'Cliente Corporativo';
};

const getServiceRoute = (srv: Service) => {
  return srv.clientAddress || srv.description || '';
};

const formatDate = (dateStr?: string) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
};

const goToServices = () => {
  router.push('/services');
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

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(16);
    doc.setTextColor(21, 128, 61);
    doc.text('TRANSPORTE TITO', 14, 15);

    doc.setFontSize(8);
    doc.setFont('helvetica', 'italic');
    doc.setTextColor(110, 110, 110);
    doc.text('"Servirle es nuestra prioridad" — R.D.', 14, 20);

    doc.setFontSize(13);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(20, 20, 20);
    const title = isAllDrivers 
      ? 'INFORME DE SERVICIOS — FLOTA COMPLETA' 
      : `INFORME DE SERVICIOS REALIZADOS — ${driverLabel.toUpperCase()}`;
    doc.text(title, 14, 28);

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
    <!-- VISTA 1: PANEL EJECUTIVO DEL CHOFER (SIN REDUNDANCIA)    -->
    <!-- ======================================================== -->
    <template v-if="!isAdmin">
      <!-- Cabecera del Chofer con alto contraste -->
      <div class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-card via-brand-card to-brand-black border border-brand-green/35 p-4 md:p-5 shadow-card flex items-center justify-between flex-wrap gap-4 before:absolute before:top-0 before:left-0 before:w-1.5 before:h-full before:bg-gradient-to-b before:from-brand-green-light before:to-brand-green">
        <div class="flex items-center gap-3.5">
          <div class="w-12 h-12 rounded-xl bg-brand-green/15 border border-brand-green/35 flex items-center justify-center shadow-md shadow-brand-green/10 text-brand-green-light shrink-0">
            <Car class="w-6 h-6" />
          </div>
          <div>
            <div class="inline-flex items-center gap-2 px-2.5 py-0.5 bg-brand-green/20 border border-brand-green/35 rounded-full text-xs font-bold text-brand-green-light mb-1 uppercase tracking-wider">
              <span>{{ employeeSummary?.code || currentUser?.code || 'CHO-001' }}</span>
              <span class="opacity-60">•</span>
              <span>Comisión: {{ driverCommissionRate }}%</span>
            </div>
            <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading leading-tight">
              {{ employeeSummary?.fullName || currentUser?.fullName || 'Chofer Activo' }}
            </h1>
            <p class="text-[13px] text-slate-300 mt-0.5">
              {{ employeeSummary?.role || currentUser?.role || 'Conductor' }} — {{ employeeSummary?.department || currentUser?.department || 'Operaciones' }}
            </p>
          </div>
        </div>

        <button 
          type="button"
          class="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white text-[13px] md:text-sm font-heading font-bold rounded-xl shadow-md shadow-brand-green/30 transition-all cursor-pointer active:scale-95 shrink-0"
          @click="$emit('open-new-service')"
        >
          <Plus class="w-4 h-4 stroke-[2.5]" />
          <span>Registrar Viaje</span>
        </button>
      </div>

      <!-- Bloque de Resumen Financiero del Chofer (Lo que más le importa: Comisión 25%, Cobrado del Mes y Hoy) -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-3.5">
        <!-- KPI 1: Tu Comisión del Mes (25%) + Total Cobrado -->
        <div class="md:col-span-2 bg-gradient-to-br from-emerald-950/60 via-brand-card to-brand-card border border-brand-green/45 rounded-2xl p-4 md:p-5 shadow-card flex flex-col justify-between gap-3">
          <div class="flex items-center justify-between gap-2 flex-wrap">
            <div class="flex items-center gap-2">
              <div class="w-9 h-9 rounded-xl bg-brand-green/20 border border-brand-green/40 flex items-center justify-center text-brand-green-light">
                <Wallet class="w-5 h-5" />
              </div>
              <div>
                <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">
                  Tu Comisión Ganada · Este Mes
                </span>
                <span class="text-[13px] text-slate-300">
                  Calculada al {{ driverCommissionRate }}% sobre tus viajes
                </span>
              </div>
            </div>
            <span class="px-3 py-1 rounded-full bg-brand-green/20 border border-brand-green/40 text-xs font-extrabold text-brand-green-light">
              {{ driverCommissionRate }}% Comisión
            </span>
          </div>

          <div class="my-1">
            <span class="text-3xl md:text-4xl font-extrabold font-heading text-brand-green-light tracking-tight">
              {{ formatMoney(monthTotalCommission, true) }}
            </span>
          </div>

          <div class="grid grid-cols-2 gap-3 pt-3 border-t border-brand-border/50 text-[13px]">
            <div class="bg-brand-black/45 rounded-xl p-2.5 border border-white/[0.06]">
              <span class="block text-slate-300 text-xs">Total cobrado (Mes)</span>
              <strong class="text-white font-heading text-sm md:text-base">{{ formatMoney(monthTotalBilled) }}</strong>
            </div>
            <div class="bg-brand-black/45 rounded-xl p-2.5 border border-white/[0.06]">
              <span class="block text-slate-300 text-xs">Viajes del mes</span>
              <strong class="text-white font-heading text-sm md:text-base">{{ formatTripCount(monthServices.length) }}</strong>
            </div>
          </div>
        </div>

        <!-- KPI 2 y 3: Resumen de Hoy y de la Semana -->
        <div class="flex flex-col gap-3.5">
          <!-- Viajes de Hoy -->
          <div class="flex-1 bg-brand-card border border-brand-border rounded-2xl p-4 shadow-sm flex flex-col justify-between gap-2">
            <div class="flex items-center justify-between">
              <span class="text-xs font-bold text-slate-200 uppercase tracking-wider">Actividad de Hoy</span>
              <div class="w-8 h-8 rounded-lg bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center text-emerald-400">
                <CalendarCheck class="w-4 h-4" />
              </div>
            </div>
            <div class="flex items-baseline justify-between gap-2">
              <span class="text-2xl font-extrabold font-heading text-white">{{ formatTripCount(todayServices.length) }}</span>
              <span class="text-sm font-bold text-brand-green-light">{{ formatMoney(todayTotalCommission, true) }}</span>
            </div>
            <div class="text-[13px] text-slate-300 flex items-center justify-between pt-1.5 border-t border-brand-border/40">
              <span>Cobrado hoy:</span>
              <strong class="text-slate-100">{{ formatMoney(todayTotalBilled) }}</strong>
            </div>
          </div>

          <!-- Últimos 7 días -->
          <div class="flex-1 bg-brand-card border border-brand-border rounded-2xl p-4 shadow-sm flex flex-col justify-between gap-2">
            <div class="flex items-center justify-between">
              <span class="text-xs font-bold text-slate-200 uppercase tracking-wider">Últimos 7 Días</span>
              <div class="w-8 h-8 rounded-lg bg-blue-500/15 border border-blue-500/30 flex items-center justify-center text-blue-400">
                <TrendingUp class="w-4 h-4" />
              </div>
            </div>
            <div class="flex items-baseline justify-between gap-2">
              <span class="text-2xl font-extrabold font-heading text-white">{{ formatTripCount(weekServices.length) }}</span>
              <span class="text-sm font-bold text-brand-green-light">{{ formatMoney(weekTotalCommission, true) }}</span>
            </div>
            <div class="text-[13px] text-slate-300 flex items-center justify-between pt-1.5 border-t border-brand-border/40">
              <span>Cobrado semana:</span>
              <strong class="text-slate-100">{{ formatMoney(weekTotalBilled) }}</strong>
            </div>
          </div>
        </div>
      </div>

      <!-- Actividad Reciente (Últimos 5 Viajes) con enlace al listado completo en Mis Servicios -->
      <div class="bg-brand-card border border-brand-border rounded-2xl p-4 md:p-5 shadow-card">
        <div class="flex items-center justify-between flex-wrap gap-3 mb-4 pb-3 border-b border-brand-border/40">
          <div>
            <h2 class="text-base md:text-lg font-extrabold text-white font-heading tracking-tight">
              Últimos Viajes Recientes
            </h2>
            <p class="text-[13px] text-slate-300">
              Resumen de tus últimos traslados registrados
            </p>
          </div>

          <button 
            type="button"
            class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-white/[0.06] hover:bg-white/[0.1] border border-white/[0.12] text-[13px] font-semibold text-brand-green-light hover:text-white transition-all cursor-pointer"
            @click="goToServices"
          >
            <span>Ver todos mis servicios ({{ driverAllServices.length }})</span>
            <ArrowRight class="w-4 h-4" />
          </button>
        </div>

        <!-- Estado de carga -->
        <div v-if="loading" class="py-10 text-center flex flex-col items-center justify-center gap-2.5">
          <div class="w-7 h-7 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
          <p class="text-sm text-slate-300">Cargando resumen de viajes...</p>
        </div>

        <!-- Sin viajes aún -->
        <div v-else-if="recentDriverServices.length === 0" class="py-10 text-center flex flex-col items-center justify-center gap-3 max-w-md mx-auto">
          <div class="w-12 h-12 rounded-2xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-brand-green-light">
            <Car class="w-6 h-6" />
          </div>
          <h3 class="text-base font-bold text-white">Aún no tienes viajes registrados</h3>
          <p class="text-sm text-slate-300">
            Registra tu próximo servicio para ver aquí tu resumen de ingresos y comisiones.
          </p>
          <button 
            type="button"
            class="mt-1 inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green text-white text-sm font-semibold rounded-xl shadow-md cursor-pointer"
            @click="$emit('open-new-service')"
          >
            <Plus class="w-4 h-4" />
            <span>Registrar Nuevo Viaje</span>
          </button>
        </div>

        <!-- Lista compacta y clara de los últimos 5 viajes -->
        <div v-else class="flex flex-col gap-3">
          <div 
            v-for="srv in recentDriverServices" 
            :key="srv.id"
            class="bg-brand-charcoal/90 border border-brand-border hover:border-brand-border-strong rounded-xl p-3.5 flex flex-col gap-2.5 transition-all"
          >
            <!-- Fila Superior: Fecha, Código y Estado -->
            <div class="flex items-center justify-between flex-wrap gap-2">
              <div class="flex items-center gap-2">
                <span class="px-2 py-0.5 bg-white/[0.06] border border-white/[0.1] rounded font-mono text-xs text-slate-200 font-semibold">
                  {{ srv.serviceCode }}
                </span>
                <span class="text-[13px] text-slate-200 font-medium flex items-center gap-1.5">
                  <Calendar class="w-3.5 h-3.5 text-brand-green-light" />
                  <span>{{ formatDate(srv.serviceDate) }}</span>
                </span>
              </div>
              <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                {{ srv.statusName || 'Completado' }}
              </span>
            </div>

            <!-- Cliente explícito, Botón Editar y Ruta con alto contraste -->
            <div class="flex flex-col gap-1">
              <div class="flex items-center justify-between gap-2">
                <div class="text-sm text-white flex items-baseline gap-1.5 flex-wrap">
                  <span class="text-slate-300 font-medium">Cliente:</span>
                  <strong class="font-bold text-white">{{ getClientName(srv) }}</strong>
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
              <div class="text-[13px] text-slate-200 flex items-center gap-1.5">
                <Car class="w-4 h-4 text-brand-green-light shrink-0" />
                <span class="font-medium">{{ srv.serviceTypeName }}</span>
              </div>
              <div v-if="getServiceRoute(srv)" class="text-[13px] text-slate-200 flex items-start gap-1.5">
                <MapPin class="w-4 h-4 text-slate-300 shrink-0 mt-0.5" />
                <span>{{ getServiceRoute(srv) }}</span>
              </div>
            </div>

            <!-- Desglose financiero: Cobrado, Gastos (si existen) y Tu Comisión -->
            <div class="flex items-center justify-between flex-wrap gap-3 pt-2.5 border-t border-brand-border/40 text-[13px]">
              <div class="flex items-center gap-4 flex-wrap">
                <div>
                  <span class="text-slate-300">Cobrado: </span>
                  <strong class="text-white font-heading font-bold">{{ formatMoney(srv.priceCharged) }}</strong>
                </div>

                <!-- Si hay gastos/peajes/viáticos registrados en notas, mostrarlos como Gastos con su estado (Efectivo vs Pendiente) -->
                <div 
                  v-if="srv.notes" 
                  class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-lg bg-white/[0.06] border border-white/[0.12] text-slate-200 text-xs flex-wrap"
                >
                  <Receipt class="w-3.5 h-3.5 text-slate-300 shrink-0" />
                  <span v-if="parseServiceExpense(srv.notes, srv.id).amount > 0">
                    Gastos: <strong class="text-white">{{ formatMoney(parseServiceExpense(srv.notes, srv.id).amount) }}</strong>
                    <span class="text-slate-300"> ({{ parseServiceExpense(srv.notes, srv.id).detail }})</span>
                  </span>
                  <span v-else>
                    Nota: {{ parseServiceExpense(srv.notes, srv.id).detail }}
                  </span>
                  <span
                    v-if="parseServiceExpense(srv.notes, srv.id).amount > 0"
                    :class="[
                      'px-1.5 py-0.2 rounded text-[10px] font-bold border',
                      parseServiceExpense(srv.notes, srv.id).paidInCash
                        ? 'bg-slate-500/20 border-slate-400/40 text-slate-200'
                        : 'bg-emerald-500/15 border-emerald-500/35 text-emerald-300'
                    ]"
                  >
                    {{ parseServiceExpense(srv.notes, srv.id).paidInCash ? '💵 Pagado en efectivo' : '⏳ Pendiente reembolso' }}
                  </span>
                </div>
              </div>

              <div class="flex items-center gap-1.5 bg-brand-green/15 border border-brand-green/30 px-2.5 py-1 rounded-lg">
                <span class="text-slate-200 text-xs">Tu comisión ({{ driverCommissionRate }}%):</span>
                <strong class="text-brand-green-light font-heading font-extrabold text-sm">
                  {{ formatMoney(getServiceEarnings(srv), true) }}
                </strong>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- ======================================================== -->
    <!-- VISTA 2: PANEL DE SUPERVISIÓN DE FLOTA (ADMINISTRADOR)   -->
    <!-- ======================================================== -->
    <template v-else>
      <!-- Cabecera para el ADMINISTRADOR -->
      <div class="relative overflow-hidden rounded-xl bg-gradient-to-br from-brand-card/95 to-brand-black/98 border border-white/[0.1] p-4 md:p-5 shadow-sm flex items-center justify-between flex-wrap gap-4 before:absolute before:top-0 before:left-0 before:w-1.5 before:h-full before:bg-gradient-to-b before:from-amber-400 before:to-amber-600">
        <div class="flex items-center gap-3.5">
          <div class="w-12 h-12 rounded-xl bg-amber-500/15 border border-amber-500/35 flex items-center justify-center shadow-md shadow-amber-500/10 text-amber-400 shrink-0">
            <ShieldCheck class="w-6 h-6" />
          </div>

          <div class="flex flex-col">
            <div class="flex items-center gap-2.5 flex-wrap">
              <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading tracking-tight leading-tight">
                {{ currentUser?.fullName || 'Tito González' }}
              </h1>
              <div class="flex items-center gap-1.5">
                <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-white/[0.08] border border-white/[0.15] text-slate-200">
                  {{ currentUser?.isSuperAdmin ? 'Propietario' : 'Administrador' }}
                </span>
                <span v-if="currentUser?.isSuperAdmin" class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/15 border border-amber-500/30 text-amber-300">
                  SuperAdmin
                </span>
              </div>
            </div>

            <div class="flex items-center gap-2 mt-1 text-[13px] text-slate-300 font-medium">
              <span class="font-mono px-2 py-0.5 rounded bg-white/[0.06] border border-white/[0.1] text-slate-200 text-xs">
                {{ currentUser?.code || 'ADM-001' }}
              </span>
              <span class="opacity-50">•</span>
              <span class="truncate">Supervisión y Control de Flota</span>
            </div>
          </div>
        </div>

        <div class="flex items-center shrink-0">
          <div class="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-white/[0.06] border border-white/[0.12] text-[13px] font-medium text-slate-200 select-none">
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
            </span>
            <span>Supervisión Activa</span>
          </div>
        </div>
      </div>

      <!-- Tarjetas de Resumen de Flota (Administrador) -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3.5">
        <div class="bg-brand-card border border-brand-border hover:border-brand-border-strong rounded-xl p-4 shadow-sm flex items-center justify-between gap-3 transition-all">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-10 h-10 rounded-xl bg-blue-500/15 border border-blue-500/30 flex items-center justify-center text-blue-400 shrink-0">
              <Car class="w-5 h-5" />
            </div>
            <div class="truncate">
              <span class="block text-xs font-bold text-slate-300 uppercase tracking-wider">Viajes Completados</span>
              <span class="text-[13px] text-slate-200 truncate">{{ selectedDriverLabel }}</span>
            </div>
          </div>
          <div class="text-right shrink-0">
            <span class="text-2xl md:text-3xl font-extrabold font-heading text-white">{{ totalCompletedServices }}</span>
          </div>
        </div>

        <div class="bg-gradient-to-br from-green-950/40 to-brand-card border border-brand-green/35 rounded-xl p-4 shadow-sm flex items-center justify-between gap-3 transition-all">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-10 h-10 rounded-xl bg-brand-green/20 border border-brand-green/35 flex items-center justify-center text-brand-green-light shrink-0">
              <Banknote class="w-5 h-5" />
            </div>
            <div class="truncate">
              <span class="block text-xs font-bold text-slate-300 uppercase tracking-wider">Total Facturado</span>
              <span class="text-[13px] text-brand-green-light font-medium">Volumen de flota</span>
            </div>
          </div>
          <div class="text-right shrink-0">
            <span class="text-xl md:text-2xl font-extrabold font-heading text-white tracking-tight">
              {{ formatMoney(totalBilledServices) }}
            </span>
          </div>
        </div>

        <div class="bg-brand-card border border-brand-border rounded-xl p-4 shadow-sm flex items-center justify-between gap-3 transition-all">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-10 h-10 rounded-xl bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shrink-0">
              <Wallet class="w-5 h-5" />
            </div>
            <div class="truncate">
              <span class="block text-xs font-bold text-slate-300 uppercase tracking-wider">Comisiones Choferes</span>
              <span class="text-[13px] text-slate-300">Acumulado del filtro</span>
            </div>
          </div>
          <div class="text-right shrink-0">
            <span class="text-xl md:text-2xl font-extrabold font-heading text-brand-green-light tracking-tight">
              {{ formatMoney(totalAdminCommissions) }}
            </span>
          </div>
        </div>
      </div>

      <!-- Historial de Servicios de la Flota (Administrador) -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-4 md:p-5 shadow-card">
        <div class="flex items-center justify-between flex-wrap gap-3 mb-4 pb-3 border-b border-brand-border/40">
          <div class="flex items-center gap-2.5 flex-wrap">
            <h2 class="text-base md:text-lg font-extrabold text-white font-heading tracking-tight">
              Servicios de la Flota
            </h2>
            <span class="bg-brand-green/20 text-brand-green-light px-2.5 py-0.5 rounded-full text-xs font-bold border border-brand-green/30">
              {{ formatTripCount(filteredServices.length) }}
            </span>

            <button 
              type="button"
              class="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-gradient-to-r from-emerald-600 to-brand-green hover:from-emerald-500 hover:to-brand-green-light text-white text-[13px] font-heading font-bold rounded-xl shadow-md hover:shadow-lg transition-all cursor-pointer active:scale-95 shrink-0 disabled:opacity-50 disabled:cursor-not-allowed"
              @click="exportDriverServicesReport"
              :disabled="filteredServices.length === 0 || exportingPdf"
            >
              <FileDown class="w-4 h-4" />
              <span>{{ exportingPdf ? 'Generando...' : `Generar informe PDF (${selectedDriverLabel})` }}</span>
            </button>
          </div>

          <!-- Filtros de Período y Búsqueda -->
          <div class="flex items-center gap-2.5 flex-wrap">
            <div class="flex items-center gap-1.5">
              <label class="text-xs font-bold text-slate-300 uppercase tracking-wider" for="driver-filter">Chofer:</label>
              <select 
                id="driver-filter" 
                v-model="selectedDriverId" 
                class="px-3 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-[13px] font-semibold focus:border-brand-green-light outline-none cursor-pointer transition-all"
              >
                <option value="all">Todos (Flota Completa)</option>
                <option v-for="d in driversList" :key="d.id" :value="d.id">
                  {{ d.fullName }} ({{ d.code || 'CHO' }})
                </option>
              </select>
            </div>

            <div class="flex bg-brand-black/80 border border-brand-border rounded-lg p-0.5 gap-0.5">
              <button 
                v-for="f in [
                  { key: 'todos', label: 'Todos' },
                  { key: 'hoy', label: 'Hoy' },
                  { key: 'semana', label: 'Semana' },
                  { key: 'mes', label: 'Mes' },
                  { key: 'rango', label: 'Rango' }
                ]"
                :key="f.key"
                type="button"
                :class="[
                  'px-2.5 py-1 rounded-md text-[13px] font-semibold transition-all cursor-pointer',
                  activeFilter === f.key ? 'bg-brand-green text-white shadow-sm' : 'text-slate-300 hover:text-white'
                ]" 
                @click="activeFilter = f.key"
              >
                {{ f.label }}
              </button>
            </div>

            <div v-if="activeFilter === 'rango'" class="flex items-center gap-1.5 bg-brand-charcoal border border-brand-border rounded-lg px-2.5 py-1 text-[13px]">
              <input v-model="customDateFrom" type="date" class="bg-transparent text-white text-[13px] outline-none cursor-pointer" title="Fecha inicial" />
              <span class="text-slate-300 text-xs">a</span>
              <input v-model="customDateTo" type="date" class="bg-transparent text-white text-[13px] outline-none cursor-pointer" title="Fecha final" />
            </div>

            <div class="relative flex items-center min-w-[190px] md:min-w-[230px]">
              <Search class="w-4 h-4 text-slate-300 absolute left-2.5 pointer-events-none" />
              <input 
                v-model="searchQuery" 
                type="text" 
                class="w-full pl-8 pr-7 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-[13px] placeholder-slate-400 focus:border-brand-green-light outline-none transition-all" 
                placeholder="Buscar cliente, ruta o chofer..."
              />
              <button v-if="searchQuery" type="button" class="absolute right-2 text-slate-300 hover:text-white cursor-pointer" @click="searchQuery = ''">
                <X class="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>

        <div v-if="loading" class="py-12 text-center flex flex-col items-center justify-center gap-2.5">
          <div class="w-7 h-7 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
          <p class="text-sm text-slate-300">Cargando información de viajes...</p>
        </div>

        <div v-else-if="filteredServices.length === 0" class="py-12 text-center flex flex-col items-center justify-center gap-2.5 max-w-md mx-auto">
          <FileText class="w-10 h-10 text-slate-400 mb-1" />
          <h3 class="text-base font-bold text-white">No se encontraron viajes</h3>
          <p class="text-sm text-slate-300">
            {{ selectedDriverId !== 'all' ? `El chofer ${selectedDriverLabel} no tiene viajes en este período.` : 'No hay servicios registrados en el período seleccionado.' }}
          </p>
        </div>

        <!-- Tabla para Escritorio (Desktop) -->
        <div v-else class="hidden md:block overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="border-b border-brand-border/50 text-xs font-bold text-slate-300 uppercase tracking-wider">
                <th class="py-3 px-3">Código</th>
                <th class="py-3 px-3">Chofer</th>
                <th class="py-3 px-3">Fecha</th>
                <th class="py-3 px-3">Cliente / Ref.</th>
                <th class="py-3 px-3">Ruta / Servicio</th>
                <th class="py-3 px-3">Gastos / Notas</th>
                <th class="py-3 px-3">Estado</th>
                <th class="py-3 px-3 text-right">Cobrado</th>
                <th class="py-3 px-3 text-right">Comisión</th>
                <th class="py-3 px-3 text-center">Acción</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-brand-border/25 text-[13px]">
              <tr v-for="srv in filteredServices" :key="srv.id" class="hover:bg-white/[0.03] transition-colors">
                <td class="py-3 px-3">
                  <span class="inline-block px-2 py-0.5 bg-white/[0.06] border border-white/[0.1] rounded font-mono text-xs text-slate-200 font-medium">
                    {{ srv.serviceCode }}
                  </span>
                </td>
                <td class="py-3 px-3">
                  <div class="flex items-center gap-1.5">
                    <Car class="w-4 h-4 text-brand-green-light shrink-0" />
                    <span class="font-semibold text-white">{{ srv.driverName || srv.employeeName || 'Chofer' }}</span>
                  </div>
                </td>
                <td class="py-3 px-3 text-slate-200">{{ formatDate(srv.serviceDate) }}</td>
                <td class="py-3 px-3">
                  <div class="font-semibold text-white">{{ getClientName(srv) }}</div>
                  <div v-if="srv.clientPhone" class="text-xs text-slate-300">{{ srv.clientPhone }}</div>
                </td>
                <td class="py-3 px-3">
                  <div class="font-semibold text-slate-100">{{ srv.serviceTypeName }}</div>
                  <div v-if="getServiceRoute(srv)" class="text-xs text-slate-300 truncate max-w-xs flex items-center gap-1 mt-0.5">
                    <MapPin class="w-3.5 h-3.5 text-slate-400 shrink-0" />
                    <span class="truncate">{{ getServiceRoute(srv) }}</span>
                  </div>
                </td>
                <td class="py-3 px-3">
                  <div v-if="srv.notes" class="inline-flex items-center gap-1.5 text-xs text-slate-200 bg-white/[0.06] border border-white/[0.12] rounded-lg px-2.5 py-1 max-w-xs truncate" :title="srv.notes">
                    <Receipt class="w-3.5 h-3.5 text-slate-300 shrink-0" />
                    <span v-if="parseServiceExpense(srv.notes, srv.id).amount > 0" class="truncate">
                      Gastos: <strong>{{ formatMoney(parseServiceExpense(srv.notes, srv.id).amount) }}</strong>
                      ({{ parseServiceExpense(srv.notes, srv.id).paidInCash ? 'Efectivo' : 'Planilla' }})
                    </span>
                    <span v-else class="truncate">{{ parseServiceExpense(srv.notes, srv.id).detail }}</span>
                  </div>
                  <span v-else class="text-xs text-slate-400">-</span>
                </td>
                <td class="py-3 px-3">
                  <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                    {{ srv.statusName || 'Completado' }}
                  </span>
                </td>
                <td class="py-3 px-3 text-right font-heading font-semibold text-white">
                  {{ formatMoney(srv.priceCharged) }}
                </td>
                <td class="py-3 px-3 text-right font-heading font-bold text-brand-green-light">
                  {{ formatMoney(getServiceEarnings(srv), true) }}
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
          </table>
        </div>

        <!-- Tarjetas en Móvil para el Administrador -->
        <div v-if="!loading && filteredServices.length > 0" class="md:hidden flex flex-col gap-3">
          <div 
            v-for="srv in filteredServices" 
            :key="srv.id" 
            class="bg-brand-charcoal/90 border border-brand-border rounded-xl p-3.5 flex flex-col gap-2.5"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="px-2 py-0.5 bg-white/[0.06] border border-white/[0.1] rounded font-mono text-xs text-slate-200 font-medium">
                  {{ srv.serviceCode }}
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
                <span>Conductor: <strong class="text-white">{{ srv.driverName || srv.employeeName || 'Chofer' }}</strong></span>
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
              <p v-if="getServiceRoute(srv)" class="text-[13px] text-slate-300 flex items-start gap-1">
                <MapPin class="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
                <span>{{ getServiceRoute(srv) }}</span>
              </p>

              <div v-if="srv.notes" class="text-xs text-slate-200 bg-white/[0.06] border border-white/[0.12] rounded-lg px-2.5 py-1.5 mt-1 flex items-center gap-1.5 flex-wrap">
                <Receipt class="w-3.5 h-3.5 text-slate-300 shrink-0" />
                <span v-if="parseServiceExpense(srv.notes, srv.id).amount > 0">
                  <strong>Gastos: {{ formatMoney(parseServiceExpense(srv.notes, srv.id).amount) }}</strong>
                  ({{ parseServiceExpense(srv.notes, srv.id).paidInCash ? '💵 Efectivo' : '⏳ Planilla' }}) — {{ parseServiceExpense(srv.notes, srv.id).detail }}
                </span>
                <span v-else>{{ parseServiceExpense(srv.notes, srv.id).detail }}</span>
              </div>
            </div>

            <div class="flex items-center justify-between pt-2 border-t border-brand-border/40 text-[13px]">
              <div>
                <span class="text-slate-300">Cobrado: </span>
                <strong class="text-white font-heading font-bold">{{ formatMoney(srv.priceCharged) }}</strong>
              </div>
              <div>
                <span class="text-slate-300">Comisión: </span>
                <strong class="text-brand-green-light font-heading font-bold">{{ formatMoney(getServiceEarnings(srv), true) }}</strong>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
