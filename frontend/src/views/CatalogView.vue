<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import type { ServiceType } from '../types';
import {
  ClipboardList,
  RefreshCw,
  Plus,
  Plane,
  Building2,
  MapPin,
  Search,
  MoreVertical,
  Pencil,
  Trash2,
  Power,
  Tag,
  X
} from 'lucide-vue-next';

const props = defineProps<{
  serviceTypes?: ServiceType[];
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'open-create-modal'): void;
  (e: 'edit-service-type', st: ServiceType): void;
  (e: 'toggle-service-type', st: ServiceType): void;
  (e: 'delete-service-type', id: number | string): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref('');
const selectedCategory = ref<'all' | 'aeropuerto' | 'urbano' | 'interior'>('all');
const openMenuId = ref<number | string | null>(null);
const confirmDeleteId = ref<number | string | null>(null);

const closeMenus = () => {
  openMenuId.value = null;
  confirmDeleteId.value = null;
};

const toggleMenu = (id: number | string, event: Event) => {
  event.stopPropagation();
  confirmDeleteId.value = null;
  openMenuId.value = openMenuId.value === id ? null : id;
};

onMounted(() => {
  window.addEventListener('click', closeMenus);
});

onUnmounted(() => {
  window.removeEventListener('click', closeMenus);
});

const routesList = computed(() => props.serviceTypes || []);

// Clasificación canónica única (excluyente) para que la suma de las 3 categorías siempre sea igual al total
const getRouteGroup = (st: ServiceType): 'aeropuerto' | 'urbano' | 'interior' => {
  const cat = (st.category || '').toLowerCase();
  const name = (st.name || '').toLowerCase();

  // 1. Prioridad por la categoría explícita asignada a la ruta
  if (cat.includes('aero')) return 'aeropuerto';
  if (cat.includes('interurbano') || cat.includes('interior') || cat.includes('provincia') || cat.includes('fija')) {
    return 'interior';
  }
  if (cat.includes('urbano') || cat.includes('ciudad') || cat.includes('local')) {
    return 'urbano';
  }

  // 2. Fallback por nombre de la ruta si la categoría es genérica
  if (name.includes('aero')) return 'aeropuerto';
  if (name.includes('interurbano') || name.includes('interior') || name.includes('provincia')) {
    return 'interior';
  }
  return 'urbano';
};

// Etiqueta uniforme de categoría para evitar discrepancias visuales
const getCategoryDisplayLabel = (st: ServiceType) => {
  const group = getRouteGroup(st);
  if (group === 'aeropuerto') return 'Aeropuerto';
  if (group === 'interior') return 'Interior / Interurbano';
  return 'Urbano / Ciudad';
};

// Métricas de catálogo
const totalRoutes = computed(() => routesList.value.length);

const averagePrice = computed(() => {
  if (!routesList.value.length) return 0;
  const sum = routesList.value.reduce((acc, s) => acc + (Number(s.defaultPrice) || 0), 0);
  return Math.round(sum / routesList.value.length);
});

const countAirport = computed(() =>
  routesList.value.filter(s => getRouteGroup(s) === 'aeropuerto').length
);

const countUrban = computed(() =>
  routesList.value.filter(s => getRouteGroup(s) === 'urbano').length
);

const countIntercity = computed(() =>
  routesList.value.filter(s => getRouteGroup(s) === 'interior').length
);

// Filtrado de servicios usando la misma clasificación canónica
const filteredServices = computed(() => {
  return routesList.value.filter((s) => {
    if (selectedCategory.value !== 'all' && getRouteGroup(s) !== selectedCategory.value) {
      return false;
    }

    if (searchQuery.value.trim()) {
      const q = searchQuery.value.toLowerCase().trim();
      const matchName = (s.name || '').toLowerCase().includes(q);
      const matchDesc = (s.description || '').toLowerCase().includes(q);
      const matchCat = (s.category || '').toLowerCase().includes(q) || getCategoryDisplayLabel(s).toLowerCase().includes(q);
      const matchPrice = String(s.defaultPrice || '').includes(q);
      return matchName || matchDesc || matchCat || matchPrice;
    }

    return true;
  });
});

const isRouteActive = (st: ServiceType) => {
  return st.isActive !== false;
};

const handleToggleActive = (st: ServiceType) => {
  openMenuId.value = null;
  emit('toggle-service-type', st);
};

const requestDelete = (id: number | string, event: Event) => {
  event.stopPropagation();
  openMenuId.value = null;
  confirmDeleteId.value = id;
};

const executeDelete = (id: number | string) => {
  emit('delete-service-type', id);
  confirmDeleteId.value = null;
};
</script>

<template>
  <div class="flex flex-col gap-5">
    <!-- ======================================================== -->
    <!-- 1. ENCABEZADO: Título + Actualizar + "+ Nueva Ruta"      -->
    <!-- ======================================================== -->
    <div class="flex items-center justify-between flex-wrap gap-4 pb-1">
      <div class="flex items-center gap-3">
        <div>
          <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading tracking-tight">
            Catálogo de Rutas & Tarifas
          </h1>
          <p class="text-[13px] text-slate-300 mt-0.5">
            Administra las tarifas base en RD$, activa o pausa rutas y organiza categorías.
          </p>
        </div>
        <button 
          type="button"
          class="w-9 h-9 rounded-xl bg-brand-card hover:bg-brand-card-hover border border-brand-border flex items-center justify-center text-slate-200 hover:text-white transition-all cursor-pointer shadow-sm"
          @click="$emit('refresh')"
          :disabled="loading"
          title="Actualizar catálogo de rutas"
        >
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" />
        </button>
      </div>

      <button 
        type="button"
        class="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-bold text-[13px] md:text-sm rounded-xl shadow-lg shadow-brand-green/30 hover:-translate-y-0.5 transition-all cursor-pointer"
        @click="$emit('open-create-modal')"
      >
        <Plus class="w-4 h-4 stroke-[2.5]" />
        <span>Nueva Ruta / Tarifa</span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 2. RESUMEN EN 2×2 CON CONTADORES EXACTOS Y MONEDA RD$     -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3">
      <!-- Tarjeta 1: Todas las rutas -->
      <button 
        type="button"
        @click="selectedCategory = 'all'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'all' 
            ? 'bg-brand-green/15 border-brand-green ring-1 ring-brand-green/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-brand-green-light shrink-0">
            <ClipboardList class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">Todas</span>
            <span class="text-xs text-slate-300">Prom: RD$ {{ averagePrice.toLocaleString('en-US') }}</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalRoutes }}
        </span>
      </button>

      <!-- Tarjeta 2: Aeropuerto -->
      <button 
        type="button"
        @click="selectedCategory = 'aeropuerto'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'aeropuerto' 
            ? 'bg-blue-500/15 border-blue-500 ring-1 ring-blue-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-blue-500/15 border border-blue-500/30 flex items-center justify-center text-blue-400 shrink-0">
            <Plane class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">Aeropuerto</span>
            <span class="text-xs text-blue-300">Traslados</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ countAirport }}
        </span>
      </button>

      <!-- Tarjeta 3: Ciudad / Urbano -->
      <button 
        type="button"
        @click="selectedCategory = 'urbano'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'urbano' 
            ? 'bg-emerald-500/15 border-emerald-500 ring-1 ring-emerald-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shrink-0">
            <Building2 class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">Urbano</span>
            <span class="text-xs text-emerald-300">Ciudad</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ countUrban }}
        </span>
      </button>

      <!-- Tarjeta 4: Interior / Interurbano -->
      <button 
        type="button"
        @click="selectedCategory = 'interior'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'interior' 
            ? 'bg-amber-500/15 border-amber-500 ring-1 ring-amber-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-amber-500/15 border border-amber-500/30 flex items-center justify-center text-amber-400 shrink-0">
            <MapPin class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">Interior</span>
            <span class="text-xs text-amber-300">Interurbano</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ countIntercity }}
        </span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 3. BUSCADOR EN TIEMPO REAL                               -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-3 flex items-center justify-between gap-3">
      <div class="relative flex-1">
        <Search class="w-4 h-4 text-slate-300 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input 
          v-model="searchQuery" 
          type="text" 
          class="w-full pl-9 pr-8 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-[13px] placeholder-slate-400 focus:border-brand-green-light outline-none transition-all"
          placeholder="Buscar ruta por nombre, categoría, descripción o tarifa..." 
        />
        <button 
          v-if="searchQuery" 
          type="button"
          class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-300 hover:text-white cursor-pointer" 
          @click="searchQuery = ''"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      <span class="text-[13px] text-slate-300 font-medium shrink-0 hidden sm:inline">
        {{ filteredServices.length }} de {{ routesList.length }} rutas
      </span>
    </div>

    <!-- ======================================================== -->
    <!-- 4. LISTA DE TARJETAS DE SERVICIOS                        -->
    <!-- ======================================================== -->
    <div v-if="loading" class="py-16 text-center flex flex-col items-center justify-center gap-3">
      <div class="w-8 h-8 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
      <p class="text-sm text-slate-300">Cargando catálogo de rutas y tarifas...</p>
    </div>

    <div v-else-if="filteredServices.length === 0" class="py-14 text-center flex flex-col items-center justify-center gap-3 max-w-md mx-auto bg-brand-card border border-brand-border rounded-2xl p-6">
      <ClipboardList class="w-10 h-10 text-slate-400" />
      <h3 class="text-base font-bold text-white">No se encontraron rutas</h3>
      <p class="text-sm text-slate-300">No hay servicios que coincidan con la categoría o la búsqueda seleccionada.</p>
      <button 
        type="button"
        class="mt-1 px-4 py-2 bg-brand-charcoal border border-brand-border hover:border-brand-border-strong text-slate-200 text-[13px] font-semibold rounded-xl transition-all cursor-pointer"
        @click="searchQuery = ''; selectedCategory = 'all'"
      >
        Restablecer Filtros
      </button>
    </div>

    <!-- Grid de Rutas Compactas -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3.5">
      <div 
        v-for="st in filteredServices" 
        :key="st.id" 
        class="bg-brand-card border border-brand-border hover:border-brand-border-strong rounded-2xl p-4 transition-all shadow-card flex flex-col justify-between relative gap-3.5"
        :class="{ 'opacity-65': !isRouteActive(st) }"
      >
        <!-- FILA SUPERIOR: Categoría + Estado + Tarifa RD$ + Menú SVG MoreVertical -->
        <div class="flex items-center justify-between gap-2">
          <div class="flex items-center gap-1.5 flex-wrap">
            <span class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/15 border border-emerald-500/30 text-emerald-300">
              <Tag class="w-3 h-3" />
              <span>{{ getCategoryDisplayLabel(st) }}</span>
            </span>

            <span 
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold border"
              :class="isRouteActive(st) ? 'bg-brand-green/15 text-brand-green-light border-brand-green/30' : 'bg-slate-700/40 text-slate-300 border-slate-600'"
            >
              <span class="w-1.5 h-1.5 rounded-full" :class="isRouteActive(st) ? 'bg-emerald-400' : 'bg-slate-400'"></span>
              <span>{{ isRouteActive(st) ? 'Activa' : 'Pausada' }}</span>
            </span>
          </div>

          <div class="flex items-center gap-2 shrink-0">
            <!-- Precio Destacado en RD$ -->
            <div class="flex items-baseline gap-1 font-heading">
              <span class="text-xs font-bold text-brand-green-light">RD$</span>
              <span class="text-lg md:text-xl font-extrabold text-white">
                {{ Number(st.defaultPrice || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}
              </span>
            </div>

            <!-- Botón de tres puntos real en SVG (MoreVertical) -->
            <div class="relative">
              <button 
                type="button"
                class="w-8 h-8 rounded-xl bg-brand-charcoal hover:bg-white/[0.1] border border-brand-border flex items-center justify-center text-slate-200 hover:text-white transition-all cursor-pointer"
                @click="toggleMenu(st.id, $event)"
                title="Opciones de ruta"
                aria-label="Opciones de ruta"
              >
                <MoreVertical class="w-4 h-4" />
              </button>

              <!-- Menú Desplegable flotante -->
              <div 
                v-if="openMenuId === st.id" 
                class="absolute right-0 top-full mt-1.5 w-48 bg-brand-charcoal border border-brand-border-strong rounded-xl shadow-elevated p-1.5 z-50 animate-fadeIn"
                @click.stop
              >
                <button 
                  type="button"
                  class="w-full text-left px-3 py-2 text-[13px] font-semibold text-slate-200 hover:text-white hover:bg-white/[0.08] rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  @click="emit('edit-service-type', st); openMenuId = null"
                >
                  <Pencil class="w-4 h-4 text-brand-green-light" />
                  <span>Editar Ruta</span>
                </button>

                <!-- Activar / Desactivar Ruta sin borrarla -->
                <button 
                  type="button"
                  class="w-full text-left px-3 py-2 text-[13px] font-semibold text-slate-200 hover:text-white hover:bg-white/[0.08] rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  @click="handleToggleActive(st)"
                >
                  <Power class="w-4 h-4" :class="isRouteActive(st) ? 'text-amber-400' : 'text-emerald-400'" />
                  <span>{{ isRouteActive(st) ? 'Desactivar Ruta' : 'Activar Ruta' }}</span>
                </button>

                <div class="h-px bg-brand-border/40 my-1"></div>

                <button 
                  type="button"
                  class="w-full text-left px-3 py-2 text-[13px] font-semibold text-red-400 hover:text-red-300 hover:bg-red-500/10 rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  @click="requestDelete(st.id, $event)"
                >
                  <Trash2 class="w-4 h-4" />
                  <span>Eliminar Ruta</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- CUERPO DE LA TARJETA: Nombre y Descripción -->
        <div class="flex flex-col gap-1">
          <h3 class="font-bold text-white text-base font-heading leading-snug">
            {{ st.name }}
          </h3>
          <p class="text-[13px] text-slate-300 line-clamp-2 leading-relaxed">
            {{ st.description || 'Tarifa estándar registrada para servicios y traslados de flota.' }}
          </p>
        </div>

        <!-- Confirmación de Soft Delete en línea si se solicita -->
        <div v-if="confirmDeleteId === st.id" class="bg-red-500/10 border border-red-500/30 rounded-xl p-3 flex flex-col gap-2 animate-fadeIn" @click.stop>
          <p class="text-xs text-red-200 font-medium">¿Seguro que deseas eliminar esta ruta del catálogo?</p>
          <div class="flex items-center justify-end gap-2">
            <button type="button" class="px-3 py-1.5 text-xs rounded-lg bg-brand-charcoal text-slate-200 hover:text-white cursor-pointer" @click="confirmDeleteId = null">
              Cancelar
            </button>
            <button type="button" class="px-3 py-1.5 text-xs rounded-lg bg-red-600 hover:bg-red-500 text-white font-semibold cursor-pointer" @click="executeDelete(st.id)">
              Confirmar
            </button>
          </div>
        </div>

        <!-- BOTONES DE ACCIÓN RÁPIDA: Editar y Activar/Pausar -->
        <div v-else class="pt-2.5 border-t border-brand-border/40 flex items-center gap-2">
          <button 
            type="button"
            class="flex-1 py-2 px-3 bg-brand-charcoal hover:bg-white/[0.08] border border-brand-border hover:border-brand-border-strong rounded-xl text-[13px] font-semibold text-slate-200 hover:text-white transition-all flex items-center justify-center gap-1.5 cursor-pointer"
            @click="emit('edit-service-type', st)"
          >
            <Pencil class="w-3.5 h-3.5" />
            <span>Editar Tarifa / Ruta</span>
          </button>

          <button 
            type="button"
            class="py-2 px-3 bg-brand-charcoal hover:bg-white/[0.08] border border-brand-border rounded-xl text-xs font-semibold transition-all flex items-center justify-center gap-1.5 cursor-pointer"
            :class="isRouteActive(st) ? 'text-amber-300 hover:border-amber-500/40' : 'text-emerald-300 hover:border-emerald-500/40'"
            @click="handleToggleActive(st)"
            :title="isRouteActive(st) ? 'Pausar temporalmente esta ruta' : 'Reactivar esta ruta'"
          >
            <Power class="w-3.5 h-3.5" />
            <span>{{ isRouteActive(st) ? 'Pausar' : 'Activar' }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
