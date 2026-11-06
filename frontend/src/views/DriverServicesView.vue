<script setup lang="ts">
import { ref, computed } from 'vue';
import type { Service, EmployeeSummary } from '../types';
import { parseServiceExpense } from '../utils/expenses';
import {
  Car,
  Plus,
  RefreshCw,
  Search,
  Calendar,
  MapPin,
  Wallet,
  Banknote,
  Receipt,
  ClipboardList,
  Pencil,
  X
} from 'lucide-vue-next';

const props = defineProps<{
  services?: Service[];
  employeeSummary?: EmployeeSummary;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'open-new-service'): void;
  (e: 'edit-service', service: Service): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref('');
const activeFilter = ref('mes'); // Por defecto 'mes' para ver el período actual

// Soporte táctil Pull-to-Refresh para dispositivos móviles
const touchStartY = ref(0);
const pullDistance = ref(0);
const isPulling = ref(false);
const PULL_THRESHOLD = 70;

const onTouchStart = (e: TouchEvent) => {
  if (window.scrollY <= 5 && e.touches.length === 1) {
    touchStartY.value = e.touches[0].clientY;
    isPulling.value = true;
  } else {
    isPulling.value = false;
  }
};

const onTouchMove = (e: TouchEvent) => {
  if (!isPulling.value) return;
  const currentY = e.touches[0].clientY;
  const delta = currentY - touchStartY.value;
  if (delta > 0 && window.scrollY <= 5) {
    pullDistance.value = Math.min(delta * 0.45, 95);
  } else {
    pullDistance.value = 0;
  }
};

const onTouchEnd = () => {
  if (!isPulling.value) return;
  if (pullDistance.value >= PULL_THRESHOLD && !props.loading) {
    emit('refresh');
  }
  pullDistance.value = 0;
  isPulling.value = false;
};

const commissionRate = computed(() => props.employeeSummary?.commissionRate || 25);

// Filtrar servicios del chofer
const filteredServices = computed(() => {
  let list = props.services || [];

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
  }

  // Búsqueda por texto (si está activa)
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(s => 
      s.clientName?.toLowerCase().includes(q) ||
      s.clientReference?.toLowerCase().includes(q) ||
      s.clientAddress?.toLowerCase().includes(q) ||
      s.description?.toLowerCase().includes(q) ||
      s.serviceTypeName?.toLowerCase().includes(q) ||
      s.serviceCode?.toLowerCase().includes(q)
    );
  }

  return list;
});

const getServiceEarnings = (srv: Service) => {
  if (srv.employeeEarnings !== undefined && srv.employeeEarnings !== null) {
    return Number(srv.employeeEarnings);
  }
  return (Number(srv.priceCharged) || 0) * (commissionRate.value / 100);
};

// Métricas del conjunto filtrado
const filteredTotalServices = computed(() => filteredServices.value.length);

const filteredTotalEarnings = computed(() => {
  return filteredServices.value.reduce((sum, s) => sum + getServiceEarnings(s), 0);
});

const filteredTotalBilled = computed(() => {
  return filteredServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0);
});

const periodLabel = computed(() => {
  switch (activeFilter.value) {
    case 'hoy': return 'Hoy';
    case 'semana': return 'Esta Semana';
    case 'mes': return 'Este Mes';
    default: return 'Histórico Total';
  }
});

const getClientName = (srv: Service) => {
  return srv.clientName || srv.clientReference || 'Cliente Corporativo';
};

const getServiceRoute = (srv: Service) => {
  return srv.clientAddress || srv.description || '';
};

const formatMoney = (amount: number | string | undefined) => {
  return 'RD$ ' + Number(amount || 0).toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
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
</script>

<template>
  <div 
    class="flex flex-col gap-4 pb-20 md:pb-6"
    @touchstart.passive="onTouchStart"
    @touchmove.passive="onTouchMove"
    @touchend.passive="onTouchEnd"
  >
    <!-- Indicador táctil de Pull-to-Refresh (Móvil) -->
    <div 
      v-if="pullDistance > 10" 
      class="flex items-center justify-center overflow-hidden transition-all duration-150"
      :style="{ height: `${pullDistance}px` }"
    >
      <div class="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-brand-card border border-brand-green/40 text-xs font-semibold text-brand-green-light shadow-md">
        <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': pullDistance >= PULL_THRESHOLD || loading }" />
        <span>{{ pullDistance >= PULL_THRESHOLD ? 'Suelta para actualizar...' : 'Desliza hacia abajo para actualizar' }}</span>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 1. ENCABEZADO: Título + Acción única "Registrar Viaje"    -->
    <!-- ======================================================== -->
    <div class="flex items-center justify-between flex-wrap gap-3 pb-1">
      <div class="flex items-center gap-2.5">
        <div>
          <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading tracking-tight">
            Mis Servicios
          </h1>
          <p class="text-[13px] text-slate-300 hidden sm:block">
            Historial completo de traslados, cobros y comisiones
          </p>
        </div>
        <button 
          type="button"
          class="hidden md:inline-flex w-9 h-9 rounded-xl bg-brand-card hover:bg-brand-card-hover border border-brand-border items-center justify-center text-slate-200 hover:text-white transition-all cursor-pointer shadow-sm"
          @click="$emit('refresh')"
          :disabled="loading"
          title="Actualizar mis servicios"
        >
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" />
        </button>
      </div>

      <!-- Único botón de registro en la vista para evitar competencia visual -->
      <button 
        type="button"
        class="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-bold text-[13px] md:text-sm rounded-xl shadow-md shadow-brand-green/30 hover:-translate-y-0.5 transition-all cursor-pointer"
        @click="$emit('open-new-service')"
      >
        <Plus class="w-4 h-4 stroke-[2.5]" />
        <span>Registrar Viaje</span>
      </button>
    </div>

    <!-- ESTADO VACÍO TOTAL: Si no hay ningún viaje registrado en absoluto -->
    <div 
      v-if="!loading && (!services || services.length === 0)" 
      class="py-14 px-6 text-center flex flex-col items-center justify-center gap-3 bg-brand-card border border-brand-border rounded-2xl max-w-md mx-auto my-4 shadow-card"
    >
      <div class="w-14 h-14 rounded-2xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-brand-green-light mb-1">
        <Car class="w-7 h-7" />
      </div>
      <h2 class="text-lg font-bold text-white font-heading">Aún no tienes viajes registrados</h2>
      <p class="text-sm text-slate-300 leading-relaxed">
        Comienza a registrar los servicios y traslados que realices para acumular tus ganancias por comisión ({{ commissionRate }}%).
      </p>
      <button 
        type="button"
        class="mt-2 inline-flex items-center gap-2 px-5 py-2.5 bg-brand-green hover:bg-brand-green-hover text-white text-sm font-semibold rounded-xl shadow-lg shadow-brand-green/30 transition-all cursor-pointer"
        @click="$emit('open-new-service')"
      >
        <Plus class="w-4 h-4 stroke-[2.5]" />
        <span>Registra tu primer viaje</span>
      </button>
    </div>

    <!-- CONTENIDO CON VIAJES REGISTRADOS -->
    <template v-else>
      <!-- ======================================================== -->
      <!-- 2. SELECTOR DE PERÍODO Y BUSCADOR DE ALTO CONTRASTE      -->
      <!-- ======================================================== -->
      <div class="flex items-center justify-between flex-wrap gap-2.5 bg-brand-card border border-brand-border rounded-xl p-2">
        <div class="flex items-center gap-2 w-full sm:w-auto">
          <span class="text-xs font-bold text-slate-300 pl-2 uppercase tracking-wider hidden sm:inline">
            Período:
          </span>
          <div class="flex items-center bg-brand-black/90 border border-brand-border rounded-lg p-0.5 gap-0.5 w-full sm:w-auto justify-between sm:justify-start">
            <button 
              v-for="tab in [
                { key: 'hoy', label: 'Hoy' },
                { key: 'semana', label: 'Esta Semana' },
                { key: 'mes', label: 'Este Mes' },
                { key: 'todos', label: 'Todos' }
              ]"
              :key="tab.key"
              type="button"
              :class="[
                'flex-1 sm:flex-none px-3 py-1.5 rounded-md text-[13px] font-semibold transition-all cursor-pointer',
                activeFilter === tab.key ? 'bg-brand-green text-white shadow-sm' : 'text-slate-300 hover:text-white'
              ]" 
              @click="activeFilter = tab.key"
            >
              {{ tab.label }}
            </button>
          </div>
        </div>

        <!-- Buscador legible en exteriores -->
        <div class="relative flex items-center min-w-[200px] w-full sm:w-auto">
          <Search class="w-4 h-4 text-slate-300 absolute left-3 pointer-events-none" />
          <input 
            v-model="searchQuery" 
            type="text" 
            class="w-full pl-9 pr-8 py-1.5 bg-brand-charcoal border border-brand-border rounded-lg text-white text-[13px] placeholder-slate-400 focus:border-brand-green-light outline-none transition-all" 
            placeholder="Buscar cliente, ruta o código..."
          />
          <button v-if="searchQuery" type="button" class="absolute right-2.5 text-slate-300 hover:text-white cursor-pointer" @click="searchQuery = ''">
            <X class="w-4 h-4" />
          </button>
        </div>
      </div>

      <!-- ======================================================== -->
      <!-- 3. BLOQUE "TOTAL COBRADO / TU COMISIÓN (25%)" ACTIVO     -->
      <!-- ======================================================== -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
        <!-- Tarjeta 1: Tu Comisión (25%) del período -->
        <div class="bg-gradient-to-br from-emerald-950/60 via-brand-card to-brand-card border border-brand-green/45 rounded-2xl p-4 shadow-card flex items-center justify-between gap-3">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-11 h-11 rounded-xl bg-brand-green/20 border border-brand-green/40 flex items-center justify-center text-brand-green-light shrink-0">
              <Wallet class="w-5 h-5" />
            </div>
            <div>
              <div class="flex items-center gap-2 flex-wrap">
                <span class="text-xs font-bold text-slate-200 uppercase tracking-wider">
                  Tu Comisión ({{ commissionRate }}%)
                </span>
                <span class="px-2 py-0.5 rounded-full bg-brand-green/20 border border-brand-green/35 text-[11px] font-bold text-brand-green-light">
                  {{ periodLabel }}
                </span>
              </div>
              <span class="text-2xl md:text-3xl font-extrabold font-heading text-brand-green-light tracking-tight block mt-0.5">
                +{{ formatMoney(filteredTotalEarnings) }}
              </span>
            </div>
          </div>
        </div>

        <!-- Tarjeta 2: Total Cobrado y Viajes del período -->
        <div class="bg-brand-card border border-brand-border rounded-2xl p-4 shadow-sm flex items-center justify-between gap-3">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-11 h-11 rounded-xl bg-blue-500/15 border border-blue-500/30 flex items-center justify-center text-blue-400 shrink-0">
              <Banknote class="w-5 h-5" />
            </div>
            <div>
              <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">
                Total Cobrado · {{ periodLabel }}
              </span>
              <span class="text-2xl md:text-3xl font-extrabold font-heading text-white tracking-tight block mt-0.5">
                {{ formatMoney(filteredTotalBilled) }}
              </span>
            </div>
          </div>
          <div class="text-right shrink-0">
            <span class="inline-block px-3 py-1 rounded-full bg-white/[0.06] border border-white/[0.12] text-xs font-bold text-slate-200">
              {{ filteredTotalServices === 1 ? '1 viaje' : `${filteredTotalServices} viajes` }}
            </span>
          </div>
        </div>
      </div>

      <!-- ======================================================== -->
      <!-- 4. LISTADO COMPLETO DE SERVICIOS REGISTRADOS             -->
      <!-- ======================================================== -->
      <div class="bg-brand-card border border-brand-border rounded-2xl p-4 md:p-5 shadow-card">
        <div class="flex items-center justify-between gap-2 mb-3.5 pb-2.5 border-b border-brand-border/40 flex-wrap">
          <div class="flex items-center gap-2">
            <h2 class="text-sm md:text-base font-bold text-white uppercase tracking-wider">
              Detalle de Viajes ({{ filteredServices.length }})
            </h2>
          </div>
          <span class="text-[13px] font-medium text-slate-300">{{ periodLabel }}</span>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="py-12 text-center flex flex-col items-center justify-center gap-2.5">
          <div class="w-7 h-7 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
          <p class="text-sm text-slate-300">Cargando viajes...</p>
        </div>

        <!-- Sin resultados con el filtro actual -->
        <div v-else-if="filteredServices.length === 0" class="py-10 text-center flex flex-col items-center justify-center gap-2.5">
          <ClipboardList class="w-9 h-9 text-slate-400" />
          <p class="text-sm text-slate-300">No hay servicios registrados en {{ periodLabel.toLowerCase() }}.</p>
          <button 
            v-if="activeFilter !== 'todos'"
            type="button"
            class="text-sm font-semibold text-brand-green-light underline cursor-pointer mt-1" 
            @click="activeFilter = 'todos'"
          >
            Ver todos los viajes
          </button>
        </div>

        <!-- Tabla en Escritorio (Desktop: md:block) -->
        <div v-else class="hidden md:block overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="border-b border-brand-border/50 text-xs font-bold text-slate-300 uppercase tracking-wider">
                <th class="py-3 px-3">Código</th>
                <th class="py-3 px-3">Fecha</th>
                <th class="py-3 px-3">Cliente / Ref.</th>
                <th class="py-3 px-3">Ruta / Detalle</th>
                <th class="py-3 px-3">Gastos / Notas</th>
                <th class="py-3 px-3">Estado</th>
                <th class="py-3 px-3 text-right">Cobrado</th>
                <th class="py-3 px-3 text-right">Tu Comisión ({{ commissionRate }}%)</th>
                <th class="py-3 px-3 text-center">Acción</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-brand-border/25 text-[13px]">
              <tr v-for="srv in filteredServices" :key="srv.id" class="hover:bg-white/[0.03] transition-colors">
                <td class="py-3 px-3">
                  <span class="px-2 py-0.5 bg-white/[0.06] border border-white/[0.1] rounded font-mono text-xs text-slate-200 font-semibold">
                    {{ srv.serviceCode }}
                  </span>
                </td>
                <td class="py-3 px-3 text-slate-200 font-medium">{{ formatDate(srv.serviceDate) }}</td>
                <td class="py-3 px-3">
                  <div class="font-bold text-white">{{ getClientName(srv) }}</div>
                  <div v-if="srv.clientPhone" class="text-xs text-slate-300">{{ srv.clientPhone }}</div>
                </td>
                <td class="py-3 px-3">
                  <div class="font-semibold text-slate-100">{{ srv.serviceTypeName }}</div>
                  <div v-if="getServiceRoute(srv)" class="text-xs text-slate-200 truncate max-w-xs flex items-center gap-1 mt-0.5">
                    <MapPin class="w-3.5 h-3.5 text-slate-300 shrink-0" />
                    <span class="truncate">{{ getServiceRoute(srv) }}</span>
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
                    <span
                      v-if="parseServiceExpense(srv.notes, srv.id).amount > 0"
                      :class="[
                        'px-2 py-0.5 rounded-full text-[10px] font-bold border',
                        parseServiceExpense(srv.notes, srv.id).paidInCash
                          ? 'bg-slate-500/20 border-slate-400/40 text-slate-200'
                          : 'bg-emerald-500/15 border-emerald-500/35 text-emerald-300'
                      ]"
                    >
                      {{ parseServiceExpense(srv.notes, srv.id).paidInCash ? '💵 Pagado en efectivo' : '⏳ Pendiente reembolso' }}
                    </span>
                  </div>
                  <span v-else class="text-xs text-slate-400">-</span>
                </td>
                <td class="py-3 px-3">
                  <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                    {{ srv.statusName || 'Completado' }}
                  </span>
                </td>
                <td class="py-3 px-3 text-right font-heading text-white font-semibold">
                  {{ formatMoney(srv.priceCharged) }}
                </td>
                <td class="py-3 px-3 text-right font-heading font-extrabold text-brand-green-light">
                  +{{ formatMoney(getServiceEarnings(srv)) }}
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

        <!-- Tarjetas en Móvil (Mobile: md:hidden) con alto contraste -->
        <div v-if="!loading && filteredServices.length > 0" class="md:hidden flex flex-col gap-3">
          <div 
            v-for="srv in filteredServices" 
            :key="srv.id" 
            class="bg-brand-charcoal/90 border border-brand-border rounded-xl p-3.5 flex flex-col gap-2.5"
          >
            <!-- Fila Superior: Fecha a la izquierda, Estado y Código a la derecha -->
            <div class="flex items-center justify-between text-[13px] pb-2 border-b border-brand-border/40">
              <div class="text-slate-200 font-medium flex items-center gap-1.5">
                <Calendar class="w-4 h-4 text-brand-green-light" />
                <span>{{ formatDate(srv.serviceDate) }}</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="px-2 py-0.5 bg-white/[0.06] border border-white/[0.1] rounded font-mono text-xs text-slate-200 font-semibold">
                  {{ srv.serviceCode }}
                </span>
                <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                  {{ srv.statusName || 'Completado' }}
                </span>
              </div>
            </div>

            <!-- Cliente explícito, Botón Editar y Ruta legible bajo el sol -->
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

              <!-- Gastos / Peajes / Viáticos con estado (Efectivo vs Pendiente reembolso) -->
              <div 
                v-if="srv.notes" 
                class="mt-1 inline-flex items-center justify-between gap-2 flex-wrap px-2.5 py-1.5 rounded-lg bg-white/[0.06] border border-white/[0.12] text-slate-200 text-xs"
              >
                <div class="flex items-center gap-1.5">
                  <Receipt class="w-3.5 h-3.5 text-slate-300 shrink-0" />
                  <span v-if="parseServiceExpense(srv.notes, srv.id).amount > 0">
                    Gastos: <strong class="text-white">{{ formatMoney(parseServiceExpense(srv.notes, srv.id).amount) }}</strong>
                    <span class="text-slate-300"> ({{ parseServiceExpense(srv.notes, srv.id).detail }})</span>
                  </span>
                  <span v-else>
                    Nota: {{ parseServiceExpense(srv.notes, srv.id).detail }}
                  </span>
                </div>
                <span
                  v-if="parseServiceExpense(srv.notes, srv.id).amount > 0"
                  :class="[
                    'px-2 py-0.5 rounded-full text-[10px] font-bold border',
                    parseServiceExpense(srv.notes, srv.id).paidInCash
                      ? 'bg-slate-500/20 border-slate-400/40 text-slate-200'
                      : 'bg-emerald-500/15 border-emerald-500/35 text-emerald-300'
                  ]"
                >
                  {{ parseServiceExpense(srv.notes, srv.id).paidInCash ? '💵 Pagado en efectivo' : '⏳ Pendiente reembolso' }}
                </span>
              </div>
            </div>

            <!-- Fila Inferior con etiquetas explícitas: Cobrado y Tu Comisión -->
            <div class="flex items-center justify-between pt-2 border-t border-brand-border/40 text-[13px]">
              <div>
                <span class="text-slate-300">Cobrado: </span>
                <strong class="text-white font-heading font-bold">{{ formatMoney(srv.priceCharged) }}</strong>
              </div>
              <div class="bg-brand-green/15 border border-brand-green/30 px-2.5 py-1 rounded-lg">
                <span class="text-slate-200 text-xs">Tu comisión: </span>
                <strong class="text-brand-green-light font-heading font-extrabold text-sm">
                  +{{ formatMoney(getServiceEarnings(srv)) }}
                </strong>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
