<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import type { User } from '../types';

const props = defineProps<{
  users?: User[];
  loading?: boolean;
  currentUser?: User | null;
}>();

const emit = defineEmits<{
  (e: 'open-create-modal'): void;
  (e: 'edit-user', u: User): void;
  (e: 'delete-user', id: number | string): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref('');
const selectedRole = ref('all'); // 'all' | 'Chofer' | 'Administrador' | 'SuperAdmin'
const onlyActive = ref(true);
const openMenuId = ref(null);

// Cerrar menú al hacer clic afuera
const closeMenu = () => {
  openMenuId.value = null;
};

const toggleMenu = (id, event) => {
  event.stopPropagation();
  openMenuId.value = openMenuId.value === id ? null : id;
};

onMounted(() => {
  window.addEventListener('click', closeMenu);
});

onUnmounted(() => {
  window.removeEventListener('click', closeMenu);
});

// Métricas de conteo
const totalDrivers = computed(() => props.users.filter(u => u.role === 'Chofer').length);
const totalAdmins = computed(() => props.users.filter(u => u.role === 'Administrador').length);
const totalSuperAdmins = computed(() => props.users.filter(u => u.role === 'SuperAdmin' || u.isSuperAdmin).length);
const activeCount = computed(() => props.users.filter(u => u.isActive).length);

// Filtrado de usuarios
const filteredUsers = computed(() => {
  return props.users.filter((u) => {
    // Filtro por rol (tarjetas de estadísticas interactivas)
    if (selectedRole.value === 'Chofer' && u.role !== 'Chofer') return false;
    if (selectedRole.value === 'Administrador' && u.role !== 'Administrador') return false;
    if (selectedRole.value === 'SuperAdmin' && !u.isSuperAdmin && u.role !== 'SuperAdmin') return false;

    // Filtro solo activos
    if (onlyActive.value && !u.isActive) return false;

    // Búsqueda por texto (nombre, correo, código, departamento)
    if (searchQuery.value.trim()) {
      const q = searchQuery.value.toLowerCase().trim();
      const matchName = (u.fullName || '').toLowerCase().includes(q);
      const matchEmail = (u.email || '').toLowerCase().includes(q);
      const matchCode = (u.code || '').toLowerCase().includes(q);
      const matchDept = (u.department || '').toLowerCase().includes(q);
      return matchName || matchEmail || matchCode || matchDept;
    }

    return true;
  });
});

// Manejo de eliminación / baja lógica
const confirmDelete = (u) => {
  openMenuId.value = null;
  if (u.id === props.currentUser?.id || u.isSuperAdmin) {
    alert('No puedes eliminar tu propia cuenta de SuperAdmin.');
    return;
  }
  const confirmed = confirm(
    `¿Confirmas dar de baja a ${u.fullName} (${u.code || 'S/C'})?\n\nEl usuario se marcará como Inactivo (Soft Delete) para preservar su historial de viajes y liquidaciones.`
  );
  if (confirmed) {
    emit('delete-user', u.id);
  }
};

// Generador de enlace de WhatsApp adaptado a RD (809, 829, 849)
const getWhatsAppUrl = (phone) => {
  if (!phone) return '#';
  let digits = phone.replace(/\D/g, '');
  if (digits.length === 10) {
    digits = '1' + digits; // Prefijo país RD (+1)
  }
  return `https://wa.me/${digits}`;
};

const sanitizePhone = (phone) => {
  if (!phone) return '';
  return phone.replace(/[^\d+]/g, '');
};

const getRoleBadgeClasses = (role, isSuperAdmin) => {
  if (isSuperAdmin || role === 'SuperAdmin') {
    return 'bg-amber-500/15 border-amber-500/30 text-amber-300';
  }
  if (role === 'Administrador') {
    return 'bg-blue-500/15 border-blue-500/30 text-blue-300';
  }
  return 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300';
};
</script>

<template>
  <div class="flex flex-col gap-5">
    <!-- ======================================================== -->
    <!-- 1. ENCABEZADO: Título + Icono ↻ + Botón "+ Nuevo Usuario" -->
    <!-- ======================================================== -->
    <div class="flex items-center justify-between flex-wrap gap-4 pb-2">
      <div class="flex items-center gap-3">
        <h1 class="text-2xl md:text-3xl font-extrabold text-white font-heading tracking-tight">
          Personal & Choferes
        </h1>
        <!-- Botón Icono Actualizar (↻) junto al título -->
        <button 
          class="w-9 h-9 rounded-xl bg-brand-card hover:bg-brand-card-hover border border-brand-border flex items-center justify-center text-slate-300 hover:text-white transition-all cursor-pointer shadow-sm"
          @click="$emit('refresh')"
          :disabled="loading"
          title="Actualizar lista de personal"
        >
          <span class="text-lg leading-none inline-block" :class="{ 'animate-spin': loading }">↻</span>
        </button>
      </div>

      <!-- Botón Principal: + Nuevo Usuario -->
      <button 
        class="inline-flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-semibold text-sm rounded-xl shadow-lg shadow-brand-green/30 hover:-translate-y-0.5 transition-all cursor-pointer"
        @click="$emit('open-create-modal')"
      >
        <span class="text-lg font-bold leading-none">+</span>
        <span>Nuevo Usuario</span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 2. RESUMEN 2×2: Tarjetas Compactas que Funcionan como Filtros -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-2.5 md:gap-3">
      <!-- Tarjeta 1: Todos los usuarios -->
      <button 
        type="button"
        @click="selectedRole = 'all'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'all' 
            ? 'bg-brand-green/15 border-brand-green ring-1 ring-brand-green/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">👥</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Todos</span>
            <span class="text-xs text-slate-400">{{ activeCount }} activos</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ users.length }}
        </span>
      </button>

      <!-- Tarjeta 2: Choferes -->
      <button 
        type="button"
        @click="selectedRole = 'Chofer'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'Chofer' 
            ? 'bg-emerald-500/15 border-emerald-500 ring-1 ring-emerald-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">🚐</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Choferes</span>
            <span class="text-xs text-emerald-400">En ruta</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalDrivers }}
        </span>
      </button>

      <!-- Tarjeta 3: Administradores -->
      <button 
        type="button"
        @click="selectedRole = 'Administrador'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'Administrador' 
            ? 'bg-blue-500/15 border-blue-500 ring-1 ring-blue-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">🛡️</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">Admins</span>
            <span class="text-xs text-blue-400">Operaciones</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalAdmins }}
        </span>
      </button>

      <!-- Tarjeta 4: SuperAdmin -->
      <button 
        type="button"
        @click="selectedRole = 'SuperAdmin'"
        :class="[
          'p-3 md:p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'SuperAdmin' 
            ? 'bg-amber-500/15 border-amber-500 ring-1 ring-amber-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <span class="text-lg shrink-0">👑</span>
          <div class="truncate">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider">SuperAdmin</span>
            <span class="text-xs text-amber-400">Dirección</span>
          </div>
        </div>
        <span class="text-xl md:text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalSuperAdmins }}
        </span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 3. BUSCADOR + INTERRUPTOR "SOLO ACTIVOS" EN LA MISMA FILA -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-3 flex items-center justify-between flex-wrap gap-3">
      <!-- Buscador -->
      <div class="relative flex-1 min-w-[240px]">
        <span class="absolute left-3 top-1/2 -translate-y-1/2 text-xs opacity-60 pointer-events-none">🔍</span>
        <input 
          v-model="searchQuery" 
          type="text" 
          class="w-full pl-8 pr-8 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-white text-xs placeholder-slate-500 focus:border-brand-green-light focus:ring-1 focus:ring-brand-green-light outline-none transition-all"
          placeholder="Buscar por nombre, correo, código (ej. CHO-001)..." 
        />
        <button 
          v-if="searchQuery" 
          class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-white text-sm" 
          @click="searchQuery = ''"
        >
          &times;
        </button>
      </div>

      <!-- Interruptor Toggle: Solo Activos -->
      <button 
        type="button" 
        role="switch" 
        :aria-checked="onlyActive" 
        @click="onlyActive = !onlyActive"
        class="inline-flex items-center gap-2.5 px-3 py-1.5 rounded-lg bg-brand-charcoal/80 border border-brand-border cursor-pointer select-none transition-all hover:border-brand-border-strong"
      >
        <span class="text-xs font-medium text-slate-300">Solo activos</span>
        <span 
          class="w-8 h-4.5 w-[34px] h-[18px] flex items-center rounded-full p-0.5 transition-colors"
          :class="onlyActive ? 'bg-brand-green' : 'bg-slate-700'"
        >
          <span 
            class="w-3.5 h-3.5 rounded-full bg-white shadow-sm transition-transform"
            :class="onlyActive ? 'translate-x-4' : 'translate-x-0'"
          ></span>
        </span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 4. LISTA DE USUARIOS (TARJETAS COMPACTAS Y MODERNAS)     -->
    <!-- ======================================================== -->

    <!-- Estado de carga -->
    <div v-if="loading" class="py-16 text-center flex flex-col items-center justify-center gap-3">
      <div class="w-8 h-8 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
      <p class="text-sm text-slate-400">Cargando personal de Transporte Tito...</p>
    </div>

    <!-- Estado vacío -->
    <div v-else-if="filteredUsers.length === 0" class="py-14 text-center flex flex-col items-center justify-center gap-3 max-w-md mx-auto bg-brand-card border border-brand-border rounded-2xl p-6">
      <div class="text-4xl">📂</div>
      <h3 class="text-base font-bold text-white">No se encontraron usuarios</h3>
      <p class="text-xs text-slate-400">No hay registros que coincidan con el rol o la búsqueda seleccionada.</p>
      <button 
        class="mt-1 px-4 py-2 bg-brand-charcoal border border-brand-border hover:border-brand-border-strong text-slate-200 text-xs font-semibold rounded-xl transition-all"
        @click="searchQuery = ''; selectedRole = 'all'; onlyActive = false"
      >
        Restablecer Filtros
      </button>
    </div>

    <!-- Cuadrícula de tarjetas de usuario compactas -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3.5">
      <div 
        v-for="u in filteredUsers" 
        :key="u.id" 
        class="bg-brand-card border border-brand-border hover:border-brand-border-strong rounded-2xl p-4 transition-all shadow-card flex flex-col justify-between relative gap-3"
        :class="{ 'opacity-60 grayscale-[30%]': !u.isActive }"
      >
        <!-- FILA SUPERIOR: Nombre, Badge de Rol, Estado y Menú ⋯ -->
        <div class="flex items-start justify-between gap-2">
          <!-- Identidad -->
          <div class="flex items-center gap-3 min-w-0">
            <!-- Avatar iniciales / corona -->
            <div 
              class="w-10 h-10 rounded-xl flex items-center justify-center font-bold text-sm shrink-0 border"
              :class="u.isSuperAdmin ? 'bg-amber-500/15 border-amber-500/30 text-amber-300' : 'bg-brand-charcoal border-brand-border text-slate-200'"
            >
              <span>{{ u.avatar || (u.isSuperAdmin ? '👑' : u.fullName.charAt(0).toUpperCase()) }}</span>
            </div>

            <div class="min-w-0">
              <div class="flex items-center gap-1.5 flex-wrap">
                <h3 class="font-bold text-white text-sm font-heading leading-tight truncate">
                  {{ u.fullName }}
                </h3>
                <span class="font-mono text-[10px] px-1.5 py-0.2 rounded bg-white/[0.05] border border-white/[0.08] text-slate-300">
                  {{ u.code || 'S/C' }}
                </span>
              </div>
              <div class="flex items-center gap-1.5 mt-1">
                <!-- Único badge de rol -->
                <span 
                  class="text-[10px] font-bold px-2 py-0.5 rounded-full border inline-block"
                  :class="getRoleBadgeClasses(u.role, u.isSuperAdmin)"
                >
                  {{ u.role }}
                </span>
              </div>
            </div>
          </div>

          <!-- Estado Activo/Inactivo + Menú ⋯ -->
          <div class="flex items-center gap-2 shrink-0">
            <!-- Estado en la cabecera -->
            <span 
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold border"
              :class="u.isActive ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30' : 'bg-slate-700/30 text-slate-400 border-slate-600/30'"
            >
              <span class="w-1.5 h-1.5 rounded-full" :class="u.isActive ? 'bg-emerald-400' : 'bg-slate-500'"></span>
              <span>{{ u.isActive ? 'Activo' : 'Inactivo' }}</span>
            </span>

            <!-- Menú de Acciones ⋯ -->
            <div class="relative">
              <button 
                class="w-7 h-7 rounded-lg bg-brand-charcoal hover:bg-white/[0.1] border border-brand-border flex items-center justify-center text-slate-300 hover:text-white transition-all text-sm font-bold cursor-pointer"
                @click="toggleMenu(u.id, $event)"
                title="Opciones de usuario"
              >
                ⋯
              </button>

              <!-- Menú Desplegable flotante -->
              <div 
                v-if="openMenuId === u.id" 
                class="absolute right-0 top-full mt-1.5 w-44 bg-brand-charcoal border border-brand-border-strong rounded-xl shadow-elevated p-1 z-50 animate-fadeIn"
                @click.stop
              >
                <button 
                  class="w-full text-left px-3 py-2 text-xs font-semibold text-slate-200 hover:text-white hover:bg-white/[0.08] rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  @click="emit('edit-user', u); openMenuId = null"
                >
                  <span>✏️</span>
                  <span>Editar Datos</span>
                </button>

                <div class="h-px bg-brand-border/40 my-1"></div>

                <button 
                  class="w-full text-left px-3 py-2 text-xs font-semibold text-red-400 hover:text-red-300 hover:bg-red-500/10 rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  :disabled="u.isSuperAdmin || u.id === currentUser?.id"
                  @click="confirmDelete(u)"
                  :title="u.isSuperAdmin ? 'No se puede dar de baja al SuperAdmin' : 'Dar de baja (Soft delete)'"
                >
                  <span>🗑️</span>
                  <span>{{ u.isActive ? 'Dar de Baja' : 'Desactivar' }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- CUERPO DE LA TARJETA: Contacto y Datos en dos columnas limpias -->
        <div class="bg-brand-charcoal/60 border border-brand-border/40 rounded-xl p-2.5 flex flex-col gap-2 text-xs">
          <!-- Correo y Teléfono / WhatsApp -->
          <div class="flex items-center justify-between flex-wrap gap-2">
            <a 
              :href="'mailto:' + u.email" 
              class="text-slate-300 hover:text-brand-green-light transition-colors flex items-center gap-1.5 truncate max-w-[200px]"
              :title="'Enviar correo a ' + u.email"
            >
              <span class="text-slate-500">✉️</span>
              <span class="truncate">{{ u.email }}</span>
            </a>

            <!-- WhatsApp Directo RD -->
            <a 
              v-if="u.phone"
              :href="getWhatsAppUrl(u.phone)" 
              target="_blank" 
              rel="noopener noreferrer" 
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-emerald-500/15 hover:bg-emerald-500/25 border border-emerald-500/30 text-emerald-400 text-[11px] font-semibold transition-all shrink-0 cursor-pointer"
              title="Abrir chat directo de WhatsApp"
            >
              <span>💬</span>
              <span>WhatsApp</span>
            </a>
          </div>

          <!-- Teléfono directo -->
          <div v-if="u.phone" class="flex items-center gap-1.5 text-slate-400">
            <span class="text-slate-500">📞</span>
            <a :href="'tel:' + sanitizePhone(u.phone)" class="hover:text-white transition-colors">
              {{ u.phone }}
            </a>
          </div>

          <!-- Ruta (para Chofer) o Departamento (para Admin) + Comisión -->
          <div class="pt-1.5 border-t border-brand-border/30 flex items-center justify-between gap-2 flex-wrap text-[11px]">
            <div class="flex items-center gap-1 text-slate-400 truncate">
              <span class="text-slate-500">{{ u.role === 'Chofer' ? '📍' : '🏢' }}</span>
              <span class="font-semibold text-slate-300">{{ u.role === 'Chofer' ? 'Ruta:' : 'Depto:' }}</span>
              <span class="truncate">{{ u.department || (u.role === 'Chofer' ? 'Ruta General' : 'Operaciones') }}</span>
            </div>

            <!-- Comisión solo para Choferes -->
            <div v-if="u.role === 'Chofer'" class="flex items-center gap-1">
              <span class="text-slate-500">Tasa:</span>
              <span class="font-bold text-brand-green-light bg-brand-green/15 border border-brand-green/30 px-1.5 py-0.2 rounded font-mono">
                {{ Number(u.commissionRate || 0).toFixed(1) }}%
              </span>
            </div>
          </div>
        </div>

        <!-- BOTÓN DE ACCIÓN RÁPIDA: Editar con estilo de botón secundario -->
        <div class="flex items-center justify-end">
          <button 
            class="w-full py-1.5 px-3 bg-brand-charcoal hover:bg-white/[0.06] border border-brand-border hover:border-brand-border-strong rounded-xl text-xs font-semibold text-slate-200 hover:text-white transition-all flex items-center justify-center gap-1.5 cursor-pointer"
            @click="emit('edit-user', u)"
          >
            <span>✏️</span>
            <span>Editar Datos</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
