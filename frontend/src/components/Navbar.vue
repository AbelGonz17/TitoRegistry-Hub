<script setup lang="ts">
import { ref, computed } from 'vue';
import type { User } from '../types';

const props = defineProps<{
  employees?: User[];
  currentEmployeeId?: number | string;
  backendOnline?: boolean;
  currentTab?: string;
  currentUser?: User | null;
}>();

defineEmits<{
  (e: 'change-employee', id: number | string): void;
  (e: 'change-tab', tab: string): void;
  (e: 'open-new-service'): void;
  (e: 'open-auth', mode: string): void;
  (e: 'logout'): void;
}>();

const showUserMenu = ref(false);

const isAdmin = computed(() => {
  return props.currentUser?.isSuperAdmin || 
         props.currentUser?.role === 'Administrador' || 
         props.currentUser?.role === 'SuperAdmin';
});

// Título dinámico según la sección actual en vista móvil
const currentSectionInfo = computed(() => {
  switch (props.currentTab) {
    case 'services':
      return {
        title: 'Mis Servicios',
        icon: '🚐'
      };
    case 'profile':
      return {
        title: 'Mi Perfil',
        icon: '👤'
      };
    case 'catalog':
      return {
        title: 'Catálogo Rutas',
        icon: '📋'
      };
    case 'users':
      return {
        title: 'Personal',
        icon: '👥'
      };
    case 'dashboard':
    default:
      return {
        title: isAdmin.value ? 'Panel de Flota' : 'Historial de Viajes',
        icon: isAdmin.value ? '👑' : '📊'
      };
  }
});
</script>

<template>
  <header class="sticky top-0 z-[800] bg-brand-charcoal/95 border-b border-brand-border/80 backdrop-blur-xl transition-all pt-[max(8px,env(safe-area-inset-top,0px))]">
    <div class="max-w-[1300px] mx-auto px-4 md:px-6 min-h-[64px] md:min-h-[72px] py-2 flex items-center justify-between gap-3 md:gap-4">
      <!-- Logo y Branding Transporte Tito -->
      <div class="flex items-center gap-3 cursor-pointer select-none py-0.5 shrink-0" @click="$emit('change-tab', 'dashboard')">
        <img src="/logo.png" alt="Transporte Tito" class="h-9 sm:h-10 md:h-11 w-auto object-contain drop-shadow-sm" />
      </div>

      <!-- Título Dinámico Central (Visible en Móvil para llenar el espacio vacío) -->
      <div class="md:hidden flex items-center justify-center min-w-0 flex-1 px-1">
        <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-white/[0.05] border border-white/[0.1] shadow-sm select-none">
          <span class="text-xs leading-none">{{ currentSectionInfo.icon }}</span>
          <span class="text-xs font-bold text-white font-heading tracking-tight truncate max-w-[160px]">
            {{ currentSectionInfo.title }}
          </span>
        </div>
      </div>

      <!-- Navegación central (visible en Desktop) -->
      <nav class="hidden md:flex items-center gap-1.5 bg-brand-black/60 border border-brand-border rounded-xl p-1">
        <!-- Panel (Para todos) -->
        <button 
          :class="['px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-2 transition-all cursor-pointer', currentTab === 'dashboard' ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30' : 'text-slate-400 hover:text-white']"
          @click="$emit('change-tab', 'dashboard')"
        >
          <span>📊</span>
          <span>{{ isAdmin ? 'Panel de Flota' : 'Mi Panel' }}</span>
        </button>

        <!-- Mis Servicios (Exclusivo Choferes) -->
        <button 
          v-if="!isAdmin"
          :class="['px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-2 transition-all cursor-pointer', currentTab === 'services' ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30' : 'text-slate-400 hover:text-white']"
          @click="$emit('change-tab', 'services')"
        >
          <span>🚐</span>
          <span>Mis Servicios</span>
        </button>

        <!-- Catálogo de Rutas (Solo Administradores) -->
        <button 
          v-if="isAdmin"
          :class="['px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-2 transition-all cursor-pointer', currentTab === 'catalog' ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30' : 'text-slate-400 hover:text-white']"
          @click="$emit('change-tab', 'catalog')"
        >
          <span>📋</span>
          <span>Catálogo de Rutas</span>
        </button>

        <!-- Personal / Choferes (Solo Administradores) -->
        <button 
          v-if="isAdmin"
          :class="['px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-2 transition-all cursor-pointer', currentTab === 'users' ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30' : 'text-slate-400 hover:text-white']"
          @click="$emit('change-tab', 'users')"
        >
          <span>👥</span>
          <span>Personal / Choferes</span>
        </button>
      </nav>

      <!-- Selector de Chofer y Acciones -->
      <div class="flex items-center gap-3">
        <!-- Indicador de conexión con el Servidor -->
        <div class="flex items-center gap-2 px-2.5 py-1.5 rounded-xl bg-brand-black/40 border border-brand-border text-xs" :title="backendOnline ? 'Servidor .NET Conectado' : 'Sin conexión al servidor'">
          <span :class="['w-2 h-2 rounded-full transition-all', backendOnline ? 'bg-brand-green-light shadow-[0_0_8px_rgba(34,197,94,0.6)]' : 'bg-red-500']"></span>
          <span class="hidden md:inline text-slate-400 font-medium">{{ backendOnline ? 'API Conectada' : 'Offline' }}</span>
        </div>

        <!-- Selector de Chofer Activo (Solo para Choferes) -->
        <div v-if="!isAdmin" class="hidden md:flex items-center gap-2 text-xs">
          <label class="font-bold text-slate-400 uppercase tracking-wider">Chofer Activo:</label>
          <select 
            class="px-3 py-1.5 bg-brand-black border border-brand-border rounded-xl text-white font-medium outline-none focus:border-brand-green-light cursor-pointer" 
            :value="currentEmployeeId" 
            @change="$emit('change-employee', Number($event.target.value))"
          >
            <option v-for="emp in employees" :key="emp.id" :value="emp.id">
              {{ emp.fullName }} ({{ emp.code }} - {{ emp.commissionRate }}%)
            </option>
          </select>
        </div>

        <!-- Botón de acción rápida: Registrar Viaje (Solo para Choferes) -->
        <button 
          v-if="!isAdmin" 
          class="hidden md:inline-flex items-center justify-center gap-1.5 px-4 py-2 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-semibold rounded-xl text-xs shadow-md shadow-brand-green/30 transition-all cursor-pointer" 
          @click="$emit('open-new-service')"
        >
          <span class="text-base leading-none font-bold">+</span>
          <span>Registrar Viaje</span>
        </button>

        <!-- Bloque de Autenticación / Perfil de Usuario -->
        <div v-if="currentUser" class="relative">
          <div 
            class="flex items-center gap-2.5 px-3 py-1.5 bg-brand-black/60 hover:bg-brand-card-hover border border-brand-border rounded-xl cursor-pointer transition-all" 
            @click="showUserMenu = !showUserMenu"
          >
            <span class="text-lg leading-none">{{ currentUser.avatar || '👤' }}</span>
            <div class="hidden md:flex flex-col text-left">
              <span class="text-xs font-bold text-white leading-tight">{{ currentUser.fullName }}</span>
              <span class="text-[10px] text-slate-400 uppercase tracking-wider">{{ currentUser.role || 'Conductor' }}</span>
            </div>
            <span class="text-slate-400 text-xs ml-0.5">▾</span>
          </div>

          <!-- Menú desplegable del usuario -->
          <div v-if="showUserMenu" class="absolute right-0 top-full mt-2 w-60 bg-brand-card border border-brand-border-strong rounded-2xl shadow-elevated p-2 z-[950]">
            <div class="p-3 bg-brand-black/40 rounded-xl mb-1.5">
              <span class="block text-xs font-bold text-white">{{ currentUser.fullName }}</span>
              <span class="block text-[11px] text-slate-400 truncate">{{ currentUser.email }}</span>
              <span 
                class="inline-block mt-2 px-2.5 py-0.5 rounded-full text-[10px] font-bold"
                :class="currentUser.isSuperAdmin ? 'bg-yellow-500/20 text-yellow-300 border border-yellow-500/30' : 'bg-brand-green/20 text-brand-green-light border border-brand-green/30'"
              >
                {{ currentUser.isSuperAdmin ? 'SUPER ADMIN 👑' : (currentUser.code || 'ACTIVO') }}
              </span>
            </div>
            <div class="h-px bg-brand-border/40 my-1"></div>
            <button 
              class="w-full text-left px-3 py-2 text-xs font-medium text-slate-300 hover:text-white hover:bg-white/[0.05] rounded-xl flex items-center gap-2 transition-colors cursor-pointer" 
              @click="$emit('change-tab', 'profile'); showUserMenu = false"
            >
              <span>👤</span>
              <span>Mi Perfil & Contraseña</span>
            </button>
            <button 
              v-if="currentUser.isSuperAdmin || currentUser.role === 'Administrador'"
              class="w-full text-left px-3 py-2 text-xs font-medium text-slate-300 hover:text-white hover:bg-white/[0.05] rounded-xl flex items-center gap-2 transition-colors cursor-pointer" 
              @click="$emit('change-tab', 'users'); showUserMenu = false"
            >
              <span>👥</span>
              <span>Personal / Choferes</span>
            </button>
            <button 
              class="w-full text-left px-3 py-2 text-xs font-medium text-red-400 hover:text-red-300 hover:bg-red-500/10 rounded-xl flex items-center gap-2 transition-colors cursor-pointer" 
              @click="$emit('logout'); showUserMenu = false"
            >
              <span>🚪</span>
              <span>Cerrar Sesión</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </header>
</template>
