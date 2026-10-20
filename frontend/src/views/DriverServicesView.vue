<script setup lang="ts">
import { ref, computed } from 'vue';
import type { Service, EmployeeSummary } from '../types';

const props = defineProps<{
  services?: Service[];
  employeeSummary?: EmployeeSummary;
  loading?: boolean;
}>();

defineEmits<{
  (e: 'open-new-service'): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref('');
const activeFilter = ref('mes'); // Por defecto 'mes' para ver el período actual

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

// Métricas del conjunto filtrado
const filteredTotalServices = computed(() => filteredServices.value.length);

const filteredTotalEarnings = computed(() => {
  return filteredServices.value.reduce((sum, s) => {
    const earnings = s.employeeEarnings !== undefined && s.employeeEarnings !== null
      ? Number(s.employeeEarnings)
      : (Number(s.priceCharged) || 0) * ((props.employeeSummary?.commissionRate || 25) / 100);
    return sum + earnings;
  }, 0);
});

const filteredTotalBilled = computed(() => {
  return filteredServices.value.reduce((sum, s) => sum + (Number(s.priceCharged) || 0), 0);
});

const averageEarnings = computed(() => {
  if (filteredTotalServices.value === 0) return 0;
  return filteredTotalEarnings.value / filteredTotalServices.value;
});

const periodLabel = computed(() => {
  switch (activeFilter.value) {
    case 'hoy': return 'Hoy';
    case 'semana': return 'Esta Semana';
    case 'mes': return 'Este Mes';
    default: return 'Total Acumulado';
  }
});

const getServiceEarnings = (srv) => {
  if (srv.employeeEarnings !== undefined && srv.employeeEarnings !== null) {
    return Number(srv.employeeEarnings);
  }
  const rate = props.employeeSummary?.commissionRate || 25;
  return (Number(srv.priceCharged) || 0) * (rate / 100);
};

const getClientName = (srv) => {
  return srv.clientName || srv.clientReference || 'Cliente Corporativo';
};

const getServiceRoute = (srv) => {
  return srv.clientAddress || srv.description || '';
};

const formatMoney = (amount) => {
  return 'RD$ ' + Number(amount || 0).toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
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
</script>

<template>
  <div class="flex flex-col gap-4 pb-20 md:pb-6">
    <!-- ======================================================== -->
    <!-- 1. ENCABEZADO: Título + ↻ + Botón "+ Registrar Viaje"     -->
    <!-- ======================================================== -->
    <div class="flex items-center justify-between flex-wrap gap-3 pb-1">
      <div class="flex items-center gap-2.5">
        <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading tracking-tight">
          Mis Servicios
        </h1>
        <button 
          class="w-8 h-8 rounded-xl bg-brand-card hover:bg-brand-card-hover border border-brand-border flex items-center justify-center text-slate-300 hover:text-white transition-all cursor-pointer shadow-sm"
          @click="$emit('refresh')"
          :disabled="loading"
          title="Actualizar mis servicios"
        >
          <span class="text-base leading-none inline-block" :class="{ 'animate-spin': loading }">↻</span>
        </button>
      </div>

      <button 
        class="inline-flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-semibold text-xs md:text-sm rounded-xl shadow-md shadow-brand-green/30 hover:-translate-y-0.5 transition-all cursor-pointer"
        @click="$emit('open-new-service')"
      >
        <span class="text-base font-bold leading-none">+</span>
        <span>Registrar Viaje</span>
      </button>
    </div>

    <!-- ESTADO VACÍO TOTAL: Si no hay ningún viaje registrado en absoluto -->
    <div 
      v-if="!loading && services.length === 0" 
      class="py-16 px-6 text-center flex flex-col items-center justify-center gap-3 bg-brand-card border border-brand-border rounded-2xl max-w-md mx-auto my-4 shadow-card"
    >
      <div class="w-16 h-16 rounded-2xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-3xl mb-1">
        🚐
      </div>
      <h2 class="text-lg font-bold text-white font-heading">Aún no tienes viajes registrados</h2>
      <p class="text-xs text-slate-400 leading-relaxed">
        Comienza a registrar los servicios y traslados que realices para acumular tus ganancias por comisión.
      </p>
      <button 
        class="mt-2 inline-flex items-center gap-2 px-5 py-2.5 bg-brand-green hover:bg-brand-green-hover text-white text-xs font-semibold rounded-xl shadow-lg shadow-brand-green/30 transition-all cursor-pointer"
        @click="$emit('open-new-service')"
      >
        <span class="font-bold text-sm">+</span>
        <span>Registra tu primer viaje</span>
      </button>
    </div>

    <!-- CONTENIDO CON VIAJES REGISTRADOS -->
    <template v-else>
      <!-- ======================================================== -->
      <!-- 2. SELECTOR DE PERÍODO ARRIBA DE LAS TARJETAS            -->
      <!-- ======================================================== -->
      <div class="flex items-center justify-between flex-wrap gap-2.5 bg-brand-card/70 border border-brand-border/60 rounded-xl p-1.5">
        <span class="text-xs font-semibold text-slate-400 pl-2 uppercase tracking-wider hidden sm:inline">
          Período:
        </span>
        <div class="flex items-center bg-brand-black/90 border border-brand-border rounded-lg p-0.5 gap-0.5 w-full sm:w-auto justify-between sm:justify-start">
          <button 
            :class="['flex-1 sm:flex-none px-3 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'hoy' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'hoy'"
          >
            Hoy
          </button>
          <button 
            :class="['flex-1 sm:flex-none px-3 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'semana' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'semana'"
          >
            Esta Semana
          </button>
          <button 
            :class="['flex-1 sm:flex-none px-3 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'mes' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'mes'"
          >
            Este Mes
          </button>
          <button 
            :class="['flex-1 sm:flex-none px-3 py-1 rounded-md text-xs font-semibold transition-all cursor-pointer', activeFilter === 'todos' ? 'bg-brand-green text-white shadow-sm' : 'text-slate-400 hover:text-white']" 
            @click="activeFilter = 'todos'"
          >
            Todos
          </button>
        </div>

        <!-- Buscador compacto (solo si hay más de 3 viajes) -->
        <div v-if="services.length > 3" class="relative flex items-center min-w-[170px] w-full sm:w-auto">
          <span class="absolute left-2.5 text-xs opacity-60">🔍</span>
          <input 
            v-model="searchQuery" 
            type="text" 
            class="w-full pl-7 pr-6 py-1 bg-brand-charcoal border border-brand-border rounded-lg text-white text-xs placeholder-slate-500 focus:border-brand-green-light outline-none transition-all" 
            placeholder="Buscar viaje..."
          />
          <button v-if="searchQuery" class="absolute right-2 text-slate-400 hover:text-white text-xs" @click="searchQuery = ''">&times;</button>
        </div>
      </div>

      <!-- ======================================================== -->
      <!-- 3. TARJETA PRINCIPAL DESTACADA: TU GANANCIA              -->
      <!-- ======================================================== -->
      <div class="bg-gradient-to-br from-green-950/40 via-brand-card to-brand-card border border-brand-green/40 hover:border-brand-green-light/50 rounded-2xl p-4 md:p-5 shadow-card transition-all">
        <div class="flex items-center justify-between gap-2 mb-1.5">
          <div class="flex items-center gap-2">
            <span class="text-xs font-bold text-slate-300 uppercase tracking-wider">
              Ganancia · {{ periodLabel }}
            </span>
            <span class="bg-brand-green/20 text-brand-green-light border border-brand-green/30 text-[10px] font-bold px-2 py-0.2 rounded-full">
              {{ employeeSummary?.commissionRate || 25 }}% Comisión
            </span>
          </div>
          <span class="text-2xl">💰</span>
        </div>

        <div class="flex items-baseline gap-1 my-1">
          <span class="text-2xl md:text-4xl font-extrabold font-heading text-white tracking-tight">
            {{ formatMoney(filteredTotalEarnings) }}
          </span>
        </div>

        <div class="flex items-center justify-between text-xs text-slate-400 pt-2 border-t border-brand-border/30 mt-2">
          <span>Monto disponible según tus comisiones</span>
          <span class="text-brand-green-light font-medium">{{ filteredTotalServices }} viajes en este período</span>
        </div>
      </div>

      <!-- ======================================================== -->
      <!-- 4. FILA DE 2 TARJETAS SECUNDARIAS: VIAJES Y FACTURADO    -->
      <!-- ======================================================== -->
      <div class="grid grid-cols-2 gap-2.5 md:gap-3">
        <!-- Tarjeta 1: Viajes Realizados -->
        <div class="bg-brand-card border border-brand-border hover:border-brand-border-strong rounded-xl p-3 md:p-3.5 shadow-sm flex flex-col justify-between transition-all">
          <div class="flex items-center justify-between gap-1 mb-1">
            <span class="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">Viajes</span>
            <span class="text-lg">🛣️</span>
          </div>
          <div class="my-0.5">
            <span class="text-xl md:text-2xl font-extrabold font-heading text-white">
              {{ filteredTotalServices }}
            </span>
          </div>
          <div class="text-[11px] text-slate-400 truncate">
            {{ filteredTotalServices > 0 ? `Promedio: ${formatMoney(averageEarnings)} / viaje` : 'Sin viajes en este período' }}
          </div>
        </div>

        <!-- Tarjeta 2: Total Facturado -->
        <div class="bg-brand-card border border-brand-border hover:border-brand-border-strong rounded-xl p-3 md:p-3.5 shadow-sm flex flex-col justify-between transition-all">
          <div class="flex items-center justify-between gap-1 mb-1">
            <span class="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">Facturado</span>
            <span class="text-lg">💵</span>
          </div>
          <div class="my-0.5">
            <span class="text-lg md:text-2xl font-extrabold font-heading text-white truncate">
              {{ formatMoney(filteredTotalBilled) }}
            </span>
          </div>
          <div class="text-[11px] text-slate-400 truncate">
            Volumen cobrado a clientes
          </div>
        </div>
      </div>

      <!-- ======================================================== -->
      <!-- 5. LISTA DE SERVICIOS REGISTRADOS                        -->
      <!-- ======================================================== -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-3.5 md:p-4 shadow-card">
        <div class="flex items-center justify-between gap-2 mb-3 pb-2 border-b border-brand-border/40 flex-wrap">
          <div class="flex items-center gap-2.5">
            <h2 class="text-sm font-bold text-white uppercase tracking-wider">
              Detalle de Viajes ({{ filteredServices.length }})
            </h2>
            <button 
              type="button"
              class="inline-flex items-center gap-1.5 px-2.5 py-1 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white text-xs font-heading font-semibold rounded-lg shadow-sm transition-all cursor-pointer active:scale-95 shrink-0"
              @click="$emit('open-new-service')"
            >
              <span class="text-sm font-bold leading-none">+</span>
              <span>Registrar Viaje</span>
            </button>
          </div>
          <span class="text-xs text-slate-400">{{ periodLabel }}</span>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="py-12 text-center flex flex-col items-center justify-center gap-2">
          <div class="w-6 h-6 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
          <p class="text-xs text-slate-400">Cargando viajes...</p>
        </div>

        <!-- Sin resultados con el filtro actual -->
        <div v-else-if="filteredServices.length === 0" class="py-10 text-center flex flex-col items-center justify-center gap-2">
          <div class="text-2xl">📋</div>
          <p class="text-xs text-slate-400">No hay servicios registrados en {{ periodLabel.toLowerCase() }}.</p>
          <button 
            v-if="activeFilter !== 'todos'"
            class="text-xs text-brand-green-light underline cursor-pointer mt-1" 
            @click="activeFilter = 'todos'"
          >
            Ver todos los viajes
          </button>
        </div>

        <!-- Tabla en Escritorio (Desktop: md:block) -->
        <div v-else class="hidden md:block overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="border-b border-brand-border/40 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                <th class="py-2.5 px-3">Código</th>
                <th class="py-2.5 px-3">Fecha</th>
                <th class="py-2.5 px-3">Cliente</th>
                <th class="py-2.5 px-3">Ruta / Servicio</th>
                <th class="py-2.5 px-3">Estado</th>
                <th class="py-2.5 px-3 text-right">Cobrado</th>
                <th class="py-2.5 px-3 text-right">Tu Ganancia</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-brand-border/20 text-xs">
              <tr v-for="srv in filteredServices" :key="srv.id" class="hover:bg-white/[0.02] transition-colors">
                <td class="py-2.5 px-3">
                  <span class="font-mono text-[11px] text-slate-400">
                    {{ srv.serviceCode }}
                  </span>
                </td>
                <td class="py-2.5 px-3 text-slate-300 font-medium">{{ formatDate(srv.serviceDate) }}</td>
                <td class="py-2.5 px-3">
                  <div class="font-semibold text-white">{{ getClientName(srv) }}</div>
                  <div v-if="srv.clientPhone" class="text-[10px] text-slate-400">{{ srv.clientPhone }}</div>
                </td>
                <td class="py-2.5 px-3">
                  <div class="font-medium text-slate-200">{{ srv.serviceTypeName }}</div>
                  <div v-if="getServiceRoute(srv)" class="text-[10px] text-slate-400 truncate max-w-xs">📍 {{ getServiceRoute(srv) }}</div>
                </td>
                <td class="py-2.5 px-3">
                  <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                    {{ srv.statusName || 'Completado' }}
                  </span>
                </td>
                <td class="py-2.5 px-3 text-right font-heading text-slate-300 font-medium">
                  {{ formatMoney(srv.priceCharged) }}
                </td>
                <td class="py-2.5 px-3 text-right font-heading font-bold text-brand-green-light">
                  +{{ formatMoney(getServiceEarnings(srv)) }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Tarjetas en Móvil (Mobile: md:hidden) -->
        <div v-if="!loading && filteredServices.length > 0" class="md:hidden flex flex-col gap-2.5">
          <div 
            v-for="srv in filteredServices" 
            :key="srv.id" 
            class="bg-brand-charcoal/80 border border-brand-border rounded-xl p-3 flex flex-col gap-2"
          >
            <!-- Fila Superior: Fecha a la izquierda, Estado y Código a la derecha -->
            <div class="flex items-center justify-between text-xs pb-1.5 border-b border-brand-border/30">
              <div class="text-slate-300 font-medium flex items-center gap-1.5">
                <span>📅</span>
                <span>{{ formatDate(srv.serviceDate) }}</span>
              </div>
              <div class="flex items-center gap-2">
                <span class="font-mono text-[10px] text-slate-400">
                  {{ srv.serviceCode }}
                </span>
                <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                  {{ srv.statusName || 'Completado' }}
                </span>
              </div>
            </div>

            <!-- Cliente y Ruta -->
            <div class="flex flex-col gap-0.5">
              <h3 class="text-sm font-bold text-white">{{ getClientName(srv) }}</h3>
              <p class="text-xs text-slate-300">🚐 {{ srv.serviceTypeName }}</p>
              <p v-if="getServiceRoute(srv)" class="text-[11px] text-slate-400 truncate">📍 {{ getServiceRoute(srv) }}</p>
            </div>

            <!-- Fila Inferior con etiquetas explícitas: Cobrado / Tu Ganancia -->
            <div class="flex items-center justify-between pt-1.5 border-t border-brand-border/30 text-xs">
              <div class="text-slate-400">
                <span>Cobrado: </span>
                <strong class="text-slate-200 font-heading font-semibold">{{ formatMoney(srv.priceCharged) }}</strong>
              </div>
              <div class="text-right">
                <span class="text-slate-400 text-[10px]">Tu ganancia: </span>
                <span class="text-brand-green-light font-heading font-bold text-xs">+{{ formatMoney(getServiceEarnings(srv)) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
