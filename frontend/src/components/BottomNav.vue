<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import type { User } from '../types';
import {
  LayoutDashboard,
  Car,
  ClipboardList,
  FileSpreadsheet,
  Users,
  User as UserIcon,
  MoreHorizontal
} from 'lucide-vue-next';

const props = defineProps<{
  currentTab?: string;
  currentUser?: User | null;
}>();

const emit = defineEmits<{
  (e: 'change-tab', tab: string): void;
  (e: 'open-new-service'): void;
  (e: 'refresh'): void;
  (e: 'open-auth', mode: string): void;
}>();

const showMoreMenu = ref(false);

const closeMoreMenu = () => {
  showMoreMenu.value = false;
};

onMounted(() => {
  window.addEventListener('click', closeMoreMenu);
});

onUnmounted(() => {
  window.removeEventListener('click', closeMoreMenu);
});

const isAdmin = computed(() => {
  return props.currentUser?.isSuperAdmin || 
         props.currentUser?.role === 'Administrador' || 
         props.currentUser?.role === 'SuperAdmin';
});

const isMoreActive = computed(() => {
  return props.currentTab === 'catalog' || props.currentTab === 'users';
});

const selectTab = (tab: string) => {
  showMoreMenu.value = false;
  emit('change-tab', tab);
};
</script>

<template>
  <nav
    aria-label="Navegación inferior"
    class="md:hidden fixed bottom-0 left-0 right-0 z-[900] bg-brand-charcoal/95 border-t border-brand-border backdrop-blur-xl px-2 py-1.5 pb-[calc(8px+env(safe-area-inset-bottom,0px))]"
  >
    <div class="flex items-center justify-around max-w-lg mx-auto relative">
      <!-- 1. Panel -->
      <button 
        type="button"
        :class="[
          'flex flex-col items-center gap-1 px-3 py-1.5 rounded-xl transition-all min-w-[60px] cursor-pointer focus:outline-none',
          currentTab === 'dashboard'
            ? 'text-brand-green-light bg-brand-green/15 font-bold'
            : 'text-slate-300 hover:text-white'
        ]"
        @click="selectTab('dashboard')"
      >
        <LayoutDashboard class="w-5 h-5" />
        <span class="text-xs font-heading tracking-tight">Panel</span>
      </button>

      <!-- 2. Mis Servicios (Solo para Choferes) -->
      <button 
        v-if="!isAdmin"
        type="button"
        :class="[
          'flex flex-col items-center gap-1 px-3 py-1.5 rounded-xl transition-all min-w-[60px] cursor-pointer focus:outline-none',
          currentTab === 'services'
            ? 'text-brand-green-light bg-brand-green/15 font-bold'
            : 'text-slate-300 hover:text-white'
        ]"
        @click="selectTab('services')"
      >
        <Car class="w-5 h-5" />
        <span class="text-xs font-heading tracking-tight">Servicios</span>
      </button>

      <!-- 2. Servicios por Chofer (Solo para Administradores) -->
      <button 
        v-if="isAdmin"
        type="button"
        :class="[
          'flex flex-col items-center gap-1 px-3 py-1.5 rounded-xl transition-all min-w-[60px] cursor-pointer focus:outline-none',
          currentTab === 'admin-services'
            ? 'text-brand-green-light bg-brand-green/15 font-bold'
            : 'text-slate-300 hover:text-white'
        ]"
        @click="selectTab('admin-services')"
      >
        <Car class="w-5 h-5" />
        <span class="text-xs font-heading tracking-tight">Choferes</span>
      </button>

      <!-- 3. Liquidaciones / Planilla (Solo para Administradores) -->
      <button 
        v-if="isAdmin"
        type="button"
        :class="[
          'flex flex-col items-center gap-1 px-3 py-1.5 rounded-xl transition-all min-w-[60px] cursor-pointer focus:outline-none',
          currentTab === 'settlements'
            ? 'text-brand-green-light bg-brand-green/15 font-bold'
            : 'text-slate-300 hover:text-white'
        ]"
        @click="selectTab('settlements')"
      >
        <FileSpreadsheet class="w-5 h-5" />
        <span class="text-xs font-heading tracking-tight">Planilla</span>
      </button>

      <!-- 4. Menú "Más" en móvil para Catálogo y Personal (Solo Administradores, evita 6 pestañas apretadas) -->
      <div v-if="isAdmin" class="relative" @click.stop>
        <button 
          type="button"
          :class="[
            'flex flex-col items-center gap-1 px-3 py-1.5 rounded-xl transition-all min-w-[60px] cursor-pointer focus:outline-none',
            isMoreActive || showMoreMenu
              ? 'text-brand-green-light bg-brand-green/15 font-bold'
              : 'text-slate-300 hover:text-white'
          ]"
          @click="showMoreMenu = !showMoreMenu"
        >
          <MoreHorizontal class="w-5 h-5" />
          <span class="text-xs font-heading tracking-tight">Más</span>
        </button>

        <!-- Menú flotante superior con Catálogo y Personal -->
        <div
          v-if="showMoreMenu"
          class="absolute bottom-full right-0 mb-2.5 w-48 bg-brand-card border border-brand-border-strong rounded-2xl shadow-elevated p-1.5 z-50 animate-fadeIn"
        >
          <button
            type="button"
            :class="[
              'w-full px-3 py-2.5 rounded-xl text-left text-[13px] font-semibold flex items-center gap-2.5 transition-colors cursor-pointer',
              currentTab === 'catalog' ? 'bg-brand-green/20 text-brand-green-light' : 'text-slate-200 hover:bg-white/[0.08]'
            ]"
            @click="selectTab('catalog')"
          >
            <ClipboardList class="w-4 h-4 text-brand-green-light" />
            <span>Catálogo de Rutas</span>
          </button>

          <button
            type="button"
            :class="[
              'w-full px-3 py-2.5 rounded-xl text-left text-[13px] font-semibold flex items-center gap-2.5 transition-colors cursor-pointer',
              currentTab === 'users' ? 'bg-brand-green/20 text-brand-green-light' : 'text-slate-200 hover:bg-white/[0.08]'
            ]"
            @click="selectTab('users')"
          >
            <Users class="w-4 h-4 text-brand-green-light" />
            <span>Personal & Choferes</span>
          </button>
        </div>
      </div>

      <!-- 5. Perfil / Cuenta en móvil -->
      <button 
        type="button"
        :class="[
          'flex flex-col items-center gap-1 px-3 py-1.5 rounded-xl transition-all min-w-[60px] cursor-pointer focus:outline-none',
          currentTab === 'profile'
            ? 'text-brand-green-light bg-brand-green/15 font-bold'
            : 'text-slate-300 hover:text-white'
        ]"
        @click="selectTab('profile')"
        title="Mi Perfil y Cuenta"
      >
        <UserIcon class="w-5 h-5" />
        <span class="text-xs font-heading tracking-tight">Perfil</span>
      </button>
    </div>
  </nav>
</template>
