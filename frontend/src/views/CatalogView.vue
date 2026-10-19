<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import type { ServiceType } from '../types';

const props = defineProps<{
  serviceTypes?: ServiceType[];
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'open-create-modal'): void;
  (e: 'edit-service-type', st: ServiceType): void;
  (e: 'delete-service-type', id: number | string): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref('');
const selectedCategory = ref('all');
const openMenuId = ref(null);
const confirmDeleteId = ref(null);

// Cerrar menús al hacer clic fuera
const closeMenus = () => {
  openMenuId.value = null;
  confirmDeleteId.value = null;
};

const toggleMenu = (id, event) => {
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

// Métricas de catálogo
const totalRoutes = computed(() => props.serviceTypes.length);

const averagePrice = computed(() => {
  if (!props.serviceTypes?.length) return 0;
  const sum = props.serviceTypes.reduce((acc, s) => acc + (Number(s.defaultPrice) || 0), 0);
  return Math.round(sum / props.serviceTypes.length);
});

const countAirport = computed(() => 
  props.serviceTypes.filter(s => (s.category || s.name || '').toLowerCase().includes('aero')).length
);

const countIntercity = computed(() => 
  props.serviceTypes.filter(s => {
    const text = (s.category || s.name || '').toLowerCase();
    return text.includes('interior') || text.includes('fija') || text.includes('interurbano');
  }).length
);

const countUrban = computed(() => 
  props.serviceTypes.filter(s => {
    const text = (s.category || s.name || '').toLowerCase();
    return text.includes('ciudad') || text.includes('urbano');
  }).length
);

// Filtrado de servicios
const filteredServices = computed(() => {
  return props.serviceTypes.filter((s) => {
    // Filtro por categoría (tarjetas de resumen)
    if (selectedCategory.value !== 'all') {
      const catLower = (s.category || '').toLowerCase();
      const nameLower = (s.name || '').toLowerCase();
      if (selectedCategory.value === 'aeropuerto' && !catLower.includes('aero') && !nameLower.includes('aero')) {
        return false;
      }
      if (selectedCategory.value === 'interior' && !catLower.includes('interior') && !catLower.includes('fija') && !nameLower.includes('interior') && !nameLower.includes('fija')) {
        return false;
      }
      if (selectedCategory.value === 'urbano' && !catLower.includes('urbano') && !catLower.includes('ciudad') && !nameLower.includes('urbano') && !nameLower.includes('ciudad')) {
        return false;
      }
    }

    // Búsqueda por texto
    if (searchQuery.value.trim()) {
      const q = searchQuery.value.toLowerCase().trim();
      const matchName = (s.name || '').toLowerCase().includes(q);
      const matchDesc = (s.description || '').toLowerCase().includes(q);
      const matchCat = (s.category || '').toLowerCase().includes(q);
      const matchPrice = String(s.defaultPrice || '').includes(q);
      return matchName || matchDesc || matchCat || matchPrice;
    }

    return true;
  });
});

const requestDelete = (id, event) => {
  event.stopPropagation();
  openMenuId.value = null;
  confirmDeleteId.value = id;
};

const executeDelete = (id) => {
  emit('delete-service-type', id);
  confirmDeleteId.value = null;
};
</script>

<template>
  <div class="flex flex-col gap-5">
    <!-- ======================================================== -->
    <!-- 1. ENCABEZADO: Título + Icono ↻ + Botón "+ Nueva Ruta"   -->
    <!-- ======================================================== -->
    <div class="flex items-center justify-between flex-wrap gap-4 pb-2">
      <div class="flex items-center gap-3">
        <h1 class="text-2xl md:text-3xl font-extrabold text-white font-heading tracking-tight">
          Catálogo de Rutas & Tarifas
        </h1>
        <!-- Botón Icono Actualizar (↻) -->
        <button 
          class="w-9 h-9 rounded-xl bg-brand-card hover:bg-brand-card-hover border border-brand-border flex items-center justify-center text-slate-300 hover:text-white transition-all cursor-pointer shadow-sm"
          @click="$emit('refresh')"
          :disabled="loading"
          title="Actualizar catálogo de rutas"
        >
          <span class="text-lg leading-none inline-block" :class="{ 'animate-spin': loading }">↻</span>
        </button>
      </div>

      <!-- Botón Principal: + Crear Nueva Ruta -->
      <button 
        class="inline-flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-semibold text-sm rounded-xl shadow-lg shadow-brand-green/30 hover:-translate-y-0.5 transition-all cursor-pointer"
        @click="$emit('open-create-modal')"
      >
        <span class="text-lg font-bold leading-none">+</span>
        <span>Nueva Ruta / Tarifa</span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 2. RESUMEN EN 2×2: Tarjetas que Funcionan como Filtros    -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-2.5 md:gap-3">
      <!-- Tarjeta 1: Todas las rutas -->
      <button 
        type="button"
        @click="selectedCategory = 'all'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'all' 
            ? 'bg-brand-green/15 border-brand-green ring-1 ring-brand-green/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">📋</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Todas</span>
            <span class="text-xs text-slate-400">Prom: ${{ averagePrice }}</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalRoutes }}
        </span>
      </button>

      <!-- Tarjeta 2: Aeropuerto -->
      <button 
        type="button"
        @click="selectedCategory = 'aeropuerto'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'aeropuerto' 
            ? 'bg-blue-500/15 border-blue-500 ring-1 ring-blue-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">✈️</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Aeropuerto</span>
            <span class="text-xs text-blue-400">Traslados</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ countAirport }}
        </span>
      </button>

      <!-- Tarjeta 3: Ciudad / Urbano -->
      <button 
        type="button"
        @click="selectedCategory = 'urbano'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'urbano' 
            ? 'bg-emerald-500/15 border-emerald-500 ring-1 ring-emerald-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">🏙️</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Urbano</span>
            <span class="text-xs text-emerald-400">Ciudad</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ countUrban }}
        </span>
      </button>

      <!-- Tarjeta 4: Interior / Rutas Fijas -->
      <button 
        type="button"
        @click="selectedCategory = 'interior'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedCategory === 'interior' 
            ? 'bg-amber-500/15 border-amber-500 ring-1 ring-amber-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">🛣️</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Interior</span>
            <span class="text-xs text-amber-400">Provincias</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ countIntercity }}
        </span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 3. BUSCADOR EN TIEMPO REAL                               -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-3 flex items-center justify-between gap-3">
      <div class="relative flex-1">
        <span class="absolute left-3 top-1/2 -translate-y-1/2 text-xs opacity-60 pointer-events-none">🔍</span>
        <input 
          v-model="searchQuery" 
          type="text" 
          class="w-full pl-8 pr-8 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-white text-xs placeholder-slate-500 focus:border-brand-green-light focus:ring-1 focus:ring-brand-green-light outline-none transition-all"
          placeholder="Buscar ruta por nombre, categoría, descripción o tarifa..." 
        />
        <button 
          v-if="searchQuery" 
          class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white text-sm" 
          @click="searchQuery = ''"
        >
          &times;
        </button>
      </div>

      <span class="text-xs text-slate-400 font-medium shrink-0 hidden sm:inline">
        {{ filteredServices.length }} de {{ serviceTypes.length }} rutas
      </span>
    </div>

    <!-- ======================================================== -->
    <!-- 4. LISTA DE TARJETAS DE SERVICIOS COMPACTAS              -->
    <!-- ======================================================== -->

    <!-- Loading State -->
    <div v-if="loading" class="py-16 text-center flex flex-col items-center justify-center gap-3">
      <div class="w-8 h-8 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
      <p class="text-sm text-slate-400">Cargando catálogo de rutas y tarifas...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredServices.length === 0" class="py-14 text-center flex flex-col items-center justify-center gap-3 max-w-md mx-auto bg-brand-card border border-brand-border rounded-2xl p-6">
      <div class="text-4xl">📋</div>
      <h3 class="text-base font-bold text-white">No se encontraron rutas</h3>
      <p class="text-xs text-slate-400">No hay servicios que coincidan con la categoría o la búsqueda seleccionada.</p>
      <button 
        class="mt-1 px-4 py-2 bg-brand-charcoal border border-brand-border hover:border-brand-border-strong text-slate-200 text-xs font-semibold rounded-xl transition-all"
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
      >
        <!-- FILA SUPERIOR: Categoría + Tarifa Destacada + Menú ⋯ -->
        <div class="flex items-center justify-between gap-2">
          <!-- Badge de categoría -->
          <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-500/15 border border-emerald-500/30 text-emerald-300">
            <span>🏷️</span>
            <span>{{ st.category || 'Servicio' }}</span>
          </span>

          <div class="flex items-center gap-2">
            <!-- Precio Destacado -->
            <div class="flex items-baseline gap-0.5 font-heading">
              <span class="text-xs font-bold text-brand-green-light">$</span>
              <span class="text-lg md:text-xl font-extrabold text-white">
                {{ Number(st.defaultPrice || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}
              </span>
            </div>

            <!-- Menú de Acciones ⋯ -->
            <div class="relative">
              <button 
                class="w-7 h-7 rounded-lg bg-brand-charcoal hover:bg-white/[0.1] border border-brand-border flex items-center justify-center text-slate-300 hover:text-white transition-all text-sm font-bold cursor-pointer"
                @click="toggleMenu(st.id, $event)"
                title="Opciones de ruta"
              >
                ⋯
              </button>

              <!-- Menú Desplegable flotante -->
              <div 
                v-if="openMenuId === st.id" 
                class="absolute right-0 top-full mt-1.5 w-44 bg-brand-charcoal border border-brand-border-strong rounded-xl shadow-elevated p-1 z-50 animate-fadeIn"
                @click.stop
              >
                <button 
                  class="w-full text-left px-3 py-2 text-xs font-semibold text-slate-200 hover:text-white hover:bg-white/[0.08] rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  @click="emit('edit-service-type', st); openMenuId = null"
                >
                  <span>✏️</span>
                  <span>Editar Ruta</span>
                </button>

                <div class="h-px bg-brand-border/40 my-1"></div>

                <button 
                  class="w-full text-left px-3 py-2 text-xs font-semibold text-red-400 hover:text-red-300 hover:bg-red-500/10 rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  @click="requestDelete(st.id, $event)"
                >
                  <span>🗑️</span>
                  <span>Dar de Baja</span>
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
          <p class="text-xs text-slate-400 line-clamp-2 leading-relaxed">
            {{ st.description || 'Tarifa estándar registrada para servicios y traslados de flota.' }}
          </p>
        </div>

        <!-- Confirmación de Soft Delete en línea si se solicita -->
        <div v-if="confirmDeleteId === st.id" class="bg-red-500/10 border border-red-500/30 rounded-xl p-3 flex flex-col gap-2 animate-fadeIn" @click.stop>
          <p class="text-xs text-red-300 font-medium">¿Seguro que deseas dar de baja (borrado lógico) esta ruta del catálogo?</p>
          <div class="flex items-center justify-end gap-2">
            <button class="px-2.5 py-1 text-xs rounded-lg bg-brand-charcoal text-slate-300 hover:text-white" @click="confirmDeleteId = null">
              Cancelar
            </button>
            <button class="px-2.5 py-1 text-xs rounded-lg bg-red-600 hover:bg-red-500 text-white font-semibold" @click="executeDelete(st.id)">
              Confirmar
            </button>
          </div>
        </div>

        <!-- BOTÓN DE ACCIÓN RÁPIDA: Editar Tarifa -->
        <div v-else class="pt-2 border-t border-brand-border/30 flex items-center justify-end">
          <button 
            class="w-full py-1.5 px-3 bg-brand-charcoal hover:bg-white/[0.06] border border-brand-border hover:border-brand-border-strong rounded-xl text-xs font-semibold text-slate-200 hover:text-white transition-all flex items-center justify-center gap-1.5 cursor-pointer"
            @click="emit('edit-service-type', st)"
          >
            <span>✏️</span>
            <span>Editar Tarifa / Ruta</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
