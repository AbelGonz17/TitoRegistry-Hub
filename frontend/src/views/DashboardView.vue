<script setup lang="ts">
import { ref, computed } from 'vue';
import type { User, Service, EmployeeSummary } from '../types';

const props = defineProps<{
  employeeSummary?: EmployeeSummary;
  services?: Service[];
  loading?: boolean;
  currentUser?: User | null;
  drivers?: User[];
}>();

defineEmits<{
  (e: 'open-new-service'): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref('');
const activeFilter = ref('todos'); // 'todos' | 'hoy' | 'semana' | 'mes'
const selectedDriverId = ref('all'); // 'all' | id del chofer

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
        <!-- Título y Contador de Viajes + Botón Registrar Viaje -->
        <div class="flex items-center gap-2.5 flex-wrap">
          <h2 class="text-base md:text-lg font-extrabold text-white font-heading tracking-tight">Historial de Viajes</h2>
          <span class="bg-brand-green/15 text-brand-green-light px-2.5 py-0.5 rounded-full text-xs font-bold border border-brand-green/20">
            {{ formatTripCount(filteredServices.length) }}
          </span>
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
          </div>

          <!-- Campo de búsqueda por cliente o ruta -->
          <div class="relative flex items-center min-w-[200px] md:min-w-[240px]">
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
              <th class="py-2.5 px-3">Estado</th>
              <th class="py-2.5 px-3 text-right">Cobrado</th>
              <th class="py-2.5 px-3 text-right">{{ isAdmin ? 'Comisión Chofer' : 'Tu Comisión' }}</th>
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
                <span :class="['status-badge', (srv.statusName || 'completado').toLowerCase()]">
                  {{ srv.statusName || 'Completado' }}
                </span>
              </td>
              <td class="py-2.5 px-3 text-right font-heading font-semibold text-slate-200">
                {{ formatMoney(srv.priceCharged) }}
              </td>
              <td class="py-2.5 px-3 text-right font-heading font-bold text-brand-green-light">
                {{ formatMoney(getServiceEarnings(srv), true) }}
              </td>
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
          </div>

          <!-- Fila Inferior con Etiquetas Explícitas de Montos -->
          <div class="flex items-center justify-between pt-2 border-t border-brand-border/30 text-[11px]">
            <div class="flex items-center gap-1">
              <span class="text-slate-400">Cobrado:</span>
              <span class="text-slate-200 font-heading font-semibold">{{ formatMoney(srv.priceCharged) }}</span>
            </div>
            <div class="flex items-center gap-1">
              <span class="text-slate-400">Tu comisión:</span>
              <span class="text-brand-green-light font-heading font-bold">{{ formatMoney(getServiceEarnings(srv), true) }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
