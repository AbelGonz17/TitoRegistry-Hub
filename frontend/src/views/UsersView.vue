<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import type { User } from '../types';
import {
  Users,
  Car,
  Shield,
  Crown,
  RefreshCw,
  Plus,
  Search,
  MoreVertical,
  Pencil,
  Trash2,
  Mail,
  Phone,
  MessageCircle,
  MapPin,
  Building2,
  X
} from 'lucide-vue-next';

const props = defineProps<{
  users?: User[];
  loading?: boolean;
  currentUser?: User | null;
}>();

const emit = defineEmits<{
  (e: 'open-create-user-modal'): void;
  (e: 'edit-user', u: User): void;
  (e: 'delete-user', id: number | string): void;
  (e: 'refresh'): void;
}>();

const searchQuery = ref('');
const selectedRole = ref<'all' | 'Chofer' | 'Administrador' | 'SuperAdmin'>('all');
const onlyActive = ref(true);
const openMenuId = ref<number | string | null>(null);

const closeMenu = () => {
  openMenuId.value = null;
};

const toggleMenu = (id: number | string, event: Event) => {
  event.stopPropagation();
  openMenuId.value = openMenuId.value === id ? null : id;
};

onMounted(() => {
  window.addEventListener('click', closeMenu);
});

onUnmounted(() => {
  window.removeEventListener('click', closeMenu);
});

const usersList = computed(() => props.users || []);

// Métricas de conteo
const totalDrivers = computed(() => usersList.value.filter(u => u.role === 'Chofer').length);
const totalAdmins = computed(() => usersList.value.filter(u => u.role === 'Administrador' && !u.isSuperAdmin).length);
const totalSuperAdmins = computed(() => usersList.value.filter(u => u.role === 'SuperAdmin' || u.isSuperAdmin).length);
const activeCount = computed(() => usersList.value.filter(u => u.isActive).length);

// Prioridad de ordenamiento por rol: 1. SuperAdmin, 2. Administrador, 3. Chofer
const getRoleSortPriority = (u: User) => {
  if (u.isSuperAdmin || u.role === 'SuperAdmin') return 1;
  if (u.role === 'Administrador') return 2;
  return 3;
};

// Filtrado y ordenamiento jerárquico de usuarios
const filteredUsers = computed(() => {
  const filtered = usersList.value.filter((u) => {
    if (selectedRole.value === 'Chofer' && u.role !== 'Chofer') return false;
    if (selectedRole.value === 'Administrador' && (u.role !== 'Administrador' || u.isSuperAdmin)) return false;
    if (selectedRole.value === 'SuperAdmin' && !u.isSuperAdmin && u.role !== 'SuperAdmin') return false;

    if (onlyActive.value && !u.isActive) return false;

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

  return [...filtered].sort((a, b) => {
    const prioDiff = getRoleSortPriority(a) - getRoleSortPriority(b);
    if (prioDiff !== 0) return prioDiff;
    return (a.fullName || '').localeCompare(b.fullName || '', 'es');
  });
});

// Manejo de eliminación / baja lógica
const confirmDelete = (u: User) => {
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
const getWhatsAppUrl = (phone?: string) => {
  if (!phone) return '#';
  let digits = phone.replace(/\D/g, '');
  if (digits.length === 10) {
    digits = '1' + digits;
  }
  return `https://wa.me/${digits}`;
};

const sanitizePhone = (phone?: string) => {
  if (!phone) return '';
  return phone.replace(/[^\d+]/g, '');
};

const getRoleBadgeClasses = (role?: string, isSuperAdmin?: boolean) => {
  if (isSuperAdmin || role === 'SuperAdmin') {
    return 'bg-amber-500/15 border-amber-500/35 text-amber-300';
  }
  if (role === 'Administrador') {
    return 'bg-blue-500/15 border-blue-500/35 text-blue-300';
  }
  return 'bg-emerald-500/15 border-emerald-500/35 text-emerald-300';
};
</script>

<template>
  <div class="flex flex-col gap-5">
    <!-- ======================================================== -->
    <!-- 1. ENCABEZADO: Título + Actualizar + "+ Nuevo Usuario"   -->
    <!-- ======================================================== -->
    <div class="flex items-center justify-between flex-wrap gap-4 pb-1">
      <div class="flex items-center gap-3">
        <div>
          <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading tracking-tight">
            Personal & Choferes
          </h1>
          <p class="text-[13px] text-slate-300 mt-0.5">
            Directorio ordenado por rol (SuperAdmin, Administración y Conductores) y estado de cuenta.
          </p>
        </div>
        <button 
          type="button"
          class="w-9 h-9 rounded-xl bg-brand-card hover:bg-brand-card-hover border border-brand-border flex items-center justify-center text-slate-200 hover:text-white transition-all cursor-pointer shadow-sm"
          @click="$emit('refresh')"
          :disabled="loading"
          title="Actualizar lista de personal"
        >
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" />
        </button>
      </div>

      <button 
        type="button"
        class="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-bold text-[13px] md:text-sm rounded-xl shadow-lg shadow-brand-green/30 hover:-translate-y-0.5 transition-all cursor-pointer"
        @click="$emit('open-create-user-modal')"
      >
        <Plus class="w-4 h-4 stroke-[2.5]" />
        <span>Nuevo Usuario</span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 2. RESUMEN 2×2 CON SUBTÍTULOS PRECISOS                   -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3">
      <!-- Tarjeta 1: Todos los usuarios -->
      <button 
        type="button"
        @click="selectedRole = 'all'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'all' 
            ? 'bg-brand-green/15 border-brand-green ring-1 ring-brand-green/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-brand-green-light shrink-0">
            <Users class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">Todos</span>
            <span class="text-xs text-slate-300">{{ activeCount }} cuentas activas</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ usersList.length }}
        </span>
      </button>

      <!-- Tarjeta 2: SuperAdmin -->
      <button 
        type="button"
        @click="selectedRole = 'SuperAdmin'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'SuperAdmin' 
            ? 'bg-amber-500/15 border-amber-500 ring-1 ring-amber-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-amber-500/15 border border-amber-500/30 flex items-center justify-center text-amber-400 shrink-0">
            <Crown class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">SuperAdmin</span>
            <span class="text-xs text-amber-300">Dirección</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalSuperAdmins }}
        </span>
      </button>

      <!-- Tarjeta 3: Administradores -->
      <button 
        type="button"
        @click="selectedRole = 'Administrador'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'Administrador' 
            ? 'bg-blue-500/15 border-blue-500 ring-1 ring-blue-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-blue-500/15 border border-blue-500/30 flex items-center justify-center text-blue-400 shrink-0">
            <Shield class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">Admins</span>
            <span class="text-xs text-blue-300">Operaciones</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalAdmins }}
        </span>
      </button>

      <!-- Tarjeta 4: Choferes (sin mezclar "En ruta" con cuenta activa) -->
      <button 
        type="button"
        @click="selectedRole = 'Chofer'"
        :class="[
          'p-3.5 rounded-xl border text-left transition-all cursor-pointer flex items-center justify-between gap-2 shadow-sm',
          selectedRole === 'Chofer' 
            ? 'bg-emerald-500/15 border-emerald-500 ring-1 ring-emerald-500/50' 
            : 'bg-brand-card border-brand-border hover:border-brand-border-strong hover:bg-brand-card-hover'
        ]"
      >
        <div class="flex items-center gap-3 min-w-0">
          <div class="w-9 h-9 rounded-xl bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shrink-0">
            <Car class="w-4 h-4" />
          </div>
          <div class="truncate">
            <span class="block text-xs font-bold text-slate-200 uppercase tracking-wider">Choferes</span>
            <span class="text-xs text-emerald-300">Conductores</span>
          </div>
        </div>
        <span class="text-2xl font-extrabold font-heading text-white shrink-0">
          {{ totalDrivers }}
        </span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 3. BUSCADOR + INTERRUPTOR "SOLO CUENTAS ACTIVAS"         -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-3 flex items-center justify-between flex-wrap gap-3">
      <div class="relative flex-1 min-w-[240px]">
        <Search class="w-4 h-4 text-slate-300 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input 
          v-model="searchQuery" 
          type="text" 
          class="w-full pl-9 pr-8 py-2 bg-brand-charcoal border border-brand-border rounded-xl text-white text-[13px] placeholder-slate-400 focus:border-brand-green-light outline-none transition-all"
          placeholder="Buscar por nombre, correo, código (ej. CHO-001)..." 
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

      <button 
        type="button" 
        role="switch" 
        :aria-checked="onlyActive" 
        @click="onlyActive = !onlyActive"
        class="inline-flex items-center gap-2.5 px-3.5 py-2 rounded-xl bg-brand-charcoal border border-brand-border cursor-pointer select-none transition-all hover:border-brand-border-strong"
      >
        <span class="text-[13px] font-semibold text-slate-200">Solo cuentas activas</span>
        <span 
          class="w-[36px] h-[20px] flex items-center rounded-full p-0.5 transition-colors"
          :class="onlyActive ? 'bg-brand-green' : 'bg-slate-700'"
        >
          <span 
            class="w-4 h-4 rounded-full bg-white shadow-sm transition-transform"
            :class="onlyActive ? 'translate-x-4' : 'translate-x-0'"
          ></span>
        </span>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- 4. LISTA DE USUARIOS ORDENADOS POR ROL                   -->
    <!-- ======================================================== -->
    <div v-if="loading" class="py-16 text-center flex flex-col items-center justify-center gap-3">
      <div class="w-8 h-8 border-2 border-brand-green border-t-transparent rounded-full animate-spin"></div>
      <p class="text-sm text-slate-300">Cargando personal de Transporte Tito...</p>
    </div>

    <div v-else-if="filteredUsers.length === 0" class="py-14 text-center flex flex-col items-center justify-center gap-3 max-w-md mx-auto bg-brand-card border border-brand-border rounded-2xl p-6">
      <Users class="w-10 h-10 text-slate-400" />
      <h3 class="text-base font-bold text-white">No se encontraron usuarios</h3>
      <p class="text-sm text-slate-300">No hay registros que coincidan con el rol o la búsqueda seleccionada.</p>
      <button 
        type="button"
        class="mt-1 px-4 py-2 bg-brand-charcoal border border-brand-border hover:border-brand-border-strong text-slate-200 text-[13px] font-semibold rounded-xl transition-all cursor-pointer"
        @click="searchQuery = ''; selectedRole = 'all'; onlyActive = false"
      >
        Restablecer Filtros
      </button>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3.5">
      <div 
        v-for="u in filteredUsers" 
        :key="u.id" 
        class="bg-brand-card border border-brand-border hover:border-brand-border-strong rounded-2xl p-4 transition-all shadow-card flex flex-col justify-between relative gap-3"
        :class="{ 'opacity-60 grayscale-[30%]': !u.isActive }"
      >
        <!-- FILA SUPERIOR: Identidad, Rol, Estado de Cuenta y Menú SVG -->
        <div class="flex items-start justify-between gap-2">
          <div class="flex items-center gap-3 min-w-0">
            <div 
              class="w-11 h-11 rounded-xl flex items-center justify-center font-bold text-sm shrink-0 border"
              :class="(u.isSuperAdmin || u.role === 'SuperAdmin') ? 'bg-amber-500/15 border-amber-500/35 text-amber-300' : (u.role === 'Administrador' ? 'bg-blue-500/15 border-blue-500/35 text-blue-300' : 'bg-brand-green/15 border-brand-green/35 text-brand-green-light')"
            >
              <Crown v-if="u.isSuperAdmin || u.role === 'SuperAdmin'" class="w-5 h-5" />
              <Shield v-else-if="u.role === 'Administrador'" class="w-5 h-5" />
              <Car v-else class="w-5 h-5" />
            </div>

            <div class="min-w-0">
              <div class="flex items-center gap-1.5 flex-wrap">
                <h3 class="font-bold text-white text-sm md:text-base font-heading leading-tight truncate">
                  {{ u.fullName }}
                </h3>
                <span class="font-mono text-xs px-2 py-0.5 rounded bg-white/[0.06] border border-white/[0.1] text-slate-200 font-semibold">
                  {{ u.code || 'S/C' }}
                </span>
              </div>
              <div class="flex items-center gap-1.5 mt-1">
                <span 
                  class="text-xs font-bold px-2.5 py-0.5 rounded-full border inline-block"
                  :class="getRoleBadgeClasses(u.role, u.isSuperAdmin)"
                >
                  {{ u.isSuperAdmin ? 'SuperAdmin' : u.role }}
                </span>
              </div>
            </div>
          </div>

          <!-- Estado Cuenta Activa/Inactiva + Menú MoreVertical -->
          <div class="flex items-center gap-2 shrink-0">
            <span 
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border"
              :class="u.isActive ? 'bg-emerald-500/15 text-emerald-300 border-emerald-500/30' : 'bg-slate-700/40 text-slate-300 border-slate-600'"
            >
              <span class="w-1.5 h-1.5 rounded-full" :class="u.isActive ? 'bg-emerald-400' : 'bg-slate-400'"></span>
              <span>{{ u.isActive ? 'Cuenta Activa' : 'Inactivo' }}</span>
            </span>

            <div class="relative">
              <button 
                type="button"
                class="w-8 h-8 rounded-xl bg-brand-charcoal hover:bg-white/[0.1] border border-brand-border flex items-center justify-center text-slate-200 hover:text-white transition-all cursor-pointer"
                @click="toggleMenu(u.id, $event)"
                title="Opciones de usuario"
                aria-label="Opciones de usuario"
              >
                <MoreVertical class="w-4 h-4" />
              </button>

              <div 
                v-if="openMenuId === u.id" 
                class="absolute right-0 top-full mt-1.5 w-48 bg-brand-charcoal border border-brand-border-strong rounded-xl shadow-elevated p-1.5 z-50 animate-fadeIn"
                @click.stop
              >
                <button 
                  type="button"
                  class="w-full text-left px-3 py-2 text-[13px] font-semibold text-slate-200 hover:text-white hover:bg-white/[0.08] rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  @click="emit('edit-user', u); openMenuId = null"
                >
                  <Pencil class="w-4 h-4 text-brand-green-light" />
                  <span>Editar Datos</span>
                </button>

                <div class="h-px bg-brand-border/40 my-1"></div>

                <button 
                  type="button"
                  class="w-full text-left px-3 py-2 text-[13px] font-semibold text-red-400 hover:text-red-300 hover:bg-red-500/10 disabled:opacity-40 disabled:cursor-not-allowed rounded-lg flex items-center gap-2 transition-colors cursor-pointer"
                  :disabled="u.isSuperAdmin || u.id === currentUser?.id"
                  @click="confirmDelete(u)"
                >
                  <Trash2 class="w-4 h-4" />
                  <span>{{ u.isActive ? 'Dar de Baja' : 'Desactivar' }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- CUERPO DE LA TARJETA: Contacto y Datos -->
        <div class="bg-brand-charcoal/80 border border-brand-border/50 rounded-xl p-3 flex flex-col gap-2 text-[13px]">
          <div class="flex items-center justify-between flex-wrap gap-2">
            <a 
              :href="'mailto:' + u.email" 
              class="text-slate-200 hover:text-brand-green-light transition-colors flex items-center gap-1.5 truncate max-w-[210px]"
              :title="'Enviar correo a ' + u.email"
            >
              <Mail class="w-3.5 h-3.5 text-slate-300 shrink-0" />
              <span class="truncate">{{ u.email }}</span>
            </a>

            <a 
              v-if="u.phone"
              :href="getWhatsAppUrl(u.phone)" 
              target="_blank" 
              rel="noopener noreferrer" 
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-500/15 hover:bg-emerald-500/25 border border-emerald-500/30 text-emerald-300 text-xs font-semibold transition-all shrink-0 cursor-pointer"
              title="Abrir chat directo de WhatsApp"
            >
              <MessageCircle class="w-3.5 h-3.5" />
              <span>WhatsApp</span>
            </a>
          </div>

          <div v-if="u.phone" class="flex items-center gap-1.5 text-slate-200">
            <Phone class="w-3.5 h-3.5 text-slate-300 shrink-0" />
            <a :href="'tel:' + sanitizePhone(u.phone)" class="hover:text-white transition-colors font-medium">
              {{ u.phone }}
            </a>
          </div>

          <div class="pt-2 border-t border-brand-border/40 flex items-center justify-between gap-2 flex-wrap text-xs">
            <div class="flex items-center gap-1.5 text-slate-300 truncate">
              <MapPin v-if="u.role === 'Chofer'" class="w-3.5 h-3.5 text-slate-300 shrink-0" />
              <Building2 v-else class="w-3.5 h-3.5 text-slate-300 shrink-0" />
              <span class="font-semibold text-slate-200">{{ u.role === 'Chofer' ? 'Zona:' : 'Depto:' }}</span>
              <span class="truncate text-slate-200">{{ u.department || (u.role === 'Chofer' ? 'Operaciones Generales' : 'Administración') }}</span>
            </div>

            <div v-if="u.role === 'Chofer'" class="flex items-center gap-1">
              <span class="text-slate-300">Tasa:</span>
              <span class="font-bold text-brand-green-light bg-brand-green/15 border border-brand-green/30 px-2 py-0.5 rounded font-mono">
                {{ Number(u.commissionRate || 0).toFixed(1) }}%
              </span>
            </div>
          </div>
        </div>

        <!-- BOTÓN DE ACCIÓN RÁPIDA -->
        <div class="flex items-center justify-end">
          <button 
            type="button"
            class="w-full py-2 px-3 bg-brand-charcoal hover:bg-white/[0.08] border border-brand-border hover:border-brand-border-strong rounded-xl text-[13px] font-semibold text-slate-200 hover:text-white transition-all flex items-center justify-center gap-1.5 cursor-pointer"
            @click="emit('edit-user', u)"
          >
            <Pencil class="w-3.5 h-3.5" />
            <span>Editar Datos</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
