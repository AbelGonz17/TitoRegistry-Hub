<script setup lang="ts">
import { ref, computed } from 'vue';
import type { User } from '../types';
import {
  LayoutDashboard,
  Car,
  ClipboardList,
  FileSpreadsheet,
  Users,
  User as UserIcon,
  Plus,
  LogOut,
  ChevronDown,
  ShieldCheck
} from 'lucide-vue-next';

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

// Título e ícono dinámico según la sección actual en vista móvil
const currentSectionInfo = computed(() => {
  switch (props.currentTab) {
    case 'services':
      return {
        title: 'Mis Servicios',
        icon: Car
      };
    case 'admin-services':
      return {
        title: 'Servicios por Chofer',
        icon: Car
      };
    case 'settlements':
      return {
        title: 'Liquidaciones & Planilla',
        icon: FileSpreadsheet
      };
    case 'profile':
      return {
        title: 'Mi Perfil',
        icon: UserIcon
      };
    case 'catalog':
      return {
        title: 'Catálogo Rutas',
        icon: ClipboardList
      };
    case 'users':
      return {
        title: 'Personal',
        icon: Users
      };
    case 'dashboard':
    default:
      return {
        title: isAdmin.value ? 'Panel de Flota' : 'Resumen Operativo',
        icon: LayoutDashboard
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

      <!-- Título Dinámico Central (Visible en Móvil) -->
      <div class="md:hidden flex items-center justify-center min-w-0 flex-1 px-1">
        <div class="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-full bg-white/[0.06] border border-white/[0.12] shadow-sm select-none">
          <component :is="currentSectionInfo.icon" class="w-4 h-4 text-brand-green-light shrink-0" />
          <span class="text-[13px] font-bold text-white font-heading tracking-tight truncate max-w-[170px]">
            {{ currentSectionInfo.title }}
          </span>
        </div>
      </div>

      <!-- Navegación central (visible en Desktop) -->
      <nav class="hidden md:flex items-center gap-1.5 bg-brand-black/60 border border-brand-border rounded-xl p-1">
        <!-- Panel (Para todos) -->
        <button 
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer focus:outline-none',
            currentTab === 'dashboard'
              ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="$emit('change-tab', 'dashboard')"
        >
          <LayoutDashboard class="w-4 h-4" />
          <span>{{ isAdmin ? 'Servicios & Flota' : 'Panel' }}</span>
        </button>

        <!-- Mis Servicios (Exclusivo Choferes) -->
        <button 
          v-if="!isAdmin"
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer focus:outline-none',
            currentTab === 'services'
              ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="$emit('change-tab', 'services')"
        >
          <Car class="w-4 h-4" />
          <span>Mis Servicios</span>
        </button>

        <!-- Servicios por Chofer (Solo Administradores) -->
        <button 
          v-if="isAdmin"
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer focus:outline-none',
            currentTab === 'admin-services'
              ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="$emit('change-tab', 'admin-services')"
        >
          <Car class="w-4 h-4" />
          <span>Servicios por Chofer</span>
        </button>

        <!-- Catálogo de Rutas (Solo Administradores) -->
        <button 
          v-if="isAdmin"
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer focus:outline-none',
            currentTab === 'catalog'
              ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="$emit('change-tab', 'catalog')"
        >
          <ClipboardList class="w-4 h-4" />
          <span>Catálogo de Rutas</span>
        </button>

        <!-- Liquidaciones y Planilla (Solo Administradores) -->
        <button 
          v-if="isAdmin"
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer focus:outline-none',
            currentTab === 'settlements'
              ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="$emit('change-tab', 'settlements')"
        >
          <FileSpreadsheet class="w-4 h-4" />
          <span>Liquidaciones</span>
        </button>

        <!-- Personal / Choferes (Solo Administradores) -->
        <button 
          v-if="isAdmin"
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer focus:outline-none',
            currentTab === 'users'
              ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="$emit('change-tab', 'users')"
        >
          <Users class="w-4 h-4" />
          <span>Personal / Choferes</span>
        </button>

        <!-- Perfil en Desktop -->
        <button 
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-[13px] font-semibold flex items-center gap-2 transition-all cursor-pointer focus:outline-none',
            currentTab === 'profile'
              ? 'bg-brand-green text-white shadow-sm shadow-brand-green/30'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="$emit('change-tab', 'profile')"
        >
          <UserIcon class="w-4 h-4" />
          <span>Perfil</span>
        </button>
      </nav>

      <!-- Acciones de la barra superior -->
      <div class="flex items-center gap-3">
        <!-- Indicador de conexión con el Servidor (Con etiqueta clara en móvil y desktop) -->
        <div 
          v-if="isAdmin" 
          class="flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl bg-brand-black/50 border border-brand-border text-xs select-none" 
          :title="backendOnline ? 'Servidor .NET Conectado y en línea' : 'Sin conexión al servidor .NET'"
        >
          <span :class="['w-2 h-2 rounded-full shrink-0 transition-all', backendOnline ? 'bg-brand-green-light shadow-[0_0_8px_rgba(34,197,94,0.6)]' : 'bg-red-500']"></span>
          <span class="text-[11px] sm:text-xs text-slate-200 font-medium">{{ backendOnline ? 'Online' : 'Offline' }}</span>
        </div>

        <!-- Botón de acción rápida: Registrar Viaje (Solo para Choferes en Desktop) -->
        <button 
          v-if="!isAdmin" 
          type="button"
          class="hidden md:inline-flex items-center justify-center gap-1.5 px-4 py-2 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover text-white font-heading font-semibold rounded-xl text-[13px] shadow-md shadow-brand-green/30 transition-all cursor-pointer focus:outline-none" 
          @click="$emit('open-new-service')"
        >
          <Plus class="w-4 h-4 stroke-[2.5]" />
          <span>Registrar Viaje</span>
        </button>

        <!-- Bloque de Autenticación / Perfil de Usuario -->
        <div v-if="currentUser" class="relative">
          <button 
            type="button"
            class="flex items-center gap-2.5 px-3 py-1.5 bg-brand-black/60 hover:bg-brand-card-hover border border-brand-border rounded-xl cursor-pointer transition-all focus:outline-none" 
            @click="showUserMenu = !showUserMenu"
          >
            <div class="w-7 h-7 rounded-lg bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-brand-green-light">
              <ShieldCheck v-if="isAdmin" class="w-4 h-4" />
              <Car v-else class="w-4 h-4" />
            </div>
            <div class="hidden md:flex flex-col text-left">
              <span class="text-[13px] font-bold text-white leading-tight">{{ currentUser.fullName || currentUser.name || 'Usuario' }}</span>
              <span class="text-[11px] text-slate-300 uppercase tracking-wider">{{ currentUser.role || 'Conductor' }}</span>
            </div>
            <ChevronDown class="w-3.5 h-3.5 text-slate-300" />
          </button>

          <!-- Menú desplegable del usuario -->
          <div v-if="showUserMenu" class="absolute right-0 top-full mt-2 w-60 bg-brand-card border border-brand-border-strong rounded-2xl shadow-elevated p-2 z-[950]">
            <div class="p-3 bg-brand-black/40 rounded-xl mb-1.5">
              <span class="block text-sm font-bold text-white">{{ currentUser.fullName || currentUser.name || 'Usuario' }}</span>
              <span class="block text-xs text-slate-300 truncate mt-0.5">{{ currentUser.email }}</span>
              <span 
                class="inline-flex items-center gap-1 mt-2 px-2.5 py-0.5 rounded-full text-[11px] font-bold"
                :class="(currentUser.isSuperAdmin || currentUser.role === 'SuperAdmin') ? 'bg-yellow-500/20 text-yellow-300 border border-yellow-500/30' : (currentUser.role === 'Administrador' ? 'bg-blue-500/20 text-blue-300 border border-blue-500/30' : 'bg-brand-green/20 text-brand-green-light border border-brand-green/30')"
              >
                {{ (currentUser.isSuperAdmin || currentUser.role === 'SuperAdmin') ? 'SUPER ADMIN' : (currentUser.role === 'Administrador' ? 'ADMINISTRADOR' : (currentUser.code || 'CHOFER')) }}
              </span>
            </div>
            <div class="h-px bg-brand-border/40 my-1"></div>
            <button 
              type="button"
              class="w-full text-left px-3 py-2.5 text-[13px] font-semibold text-red-400 hover:text-red-300 hover:bg-red-500/10 rounded-xl flex items-center gap-2 transition-colors cursor-pointer" 
              @click="$emit('logout'); showUserMenu = false"
            >
              <LogOut class="w-4 h-4" />
              <span>Cerrar Sesión</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </header>
</template>
