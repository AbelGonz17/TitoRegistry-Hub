<script setup lang="ts">
import { computed } from 'vue';
import type { User } from '../types';

const props = defineProps<{
  currentTab?: string;
  currentUser?: User | null;
}>();

defineEmits<{
  (e: 'change-tab', tab: string): void;
  (e: 'open-new-service'): void;
  (e: 'refresh'): void;
  (e: 'open-auth', mode: string): void;
}>();

const isAdmin = computed(() => {
  return props.currentUser?.isSuperAdmin || 
         props.currentUser?.role === 'Administrador' || 
         props.currentUser?.role === 'SuperAdmin';
});
</script>

<template>
  <div class="md:hidden fixed bottom-0 left-0 right-0 z-[900] bg-brand-charcoal/95 border-t border-brand-border backdrop-blur-xl px-3 py-2 pb-[calc(8px+env(safe-area-inset-bottom,0px))]">
    <div class="flex items-center justify-around max-w-lg mx-auto relative">
      <!-- 1. Panel -->
      <button 
        :class="['flex flex-col items-center gap-1 px-2.5 py-1.5 rounded-xl transition-all min-w-[54px]', currentTab === 'dashboard' ? 'text-brand-green-light' : 'text-slate-400 hover:text-white']"
        @click="$emit('change-tab', 'dashboard')"
      >
        <span class="text-xl leading-none">📊</span>
        <span class="text-[11px] font-heading font-semibold">Panel</span>
      </button>

      <!-- 2. Mis Servicios (Solo para Choferes) -->
      <button 
        v-if="!isAdmin"
        :class="['flex flex-col items-center gap-1 px-2.5 py-1.5 rounded-xl transition-all min-w-[54px]', currentTab === 'services' ? 'text-brand-green-light' : 'text-slate-400 hover:text-white']"
        @click="$emit('change-tab', 'services')"
      >
        <span class="text-xl leading-none">🚐</span>
        <span class="text-[11px] font-heading font-semibold">Servicios</span>
      </button>

      <!-- 2. Catálogo (Solo para Administradores) -->
      <button 
        v-if="isAdmin"
        :class="['flex flex-col items-center gap-1 px-2.5 py-1.5 rounded-xl transition-all min-w-[54px]', currentTab === 'catalog' ? 'text-brand-green-light' : 'text-slate-400 hover:text-white']"
        @click="$emit('change-tab', 'catalog')"
      >
        <span class="text-xl leading-none">📋</span>
        <span class="text-[11px] font-heading font-semibold">Catálogo</span>
      </button>


      <!-- 4. Personal (Solo para Administradores) -->
      <button 
        v-if="isAdmin"
        :class="['flex flex-col items-center gap-1 px-2.5 py-1.5 rounded-xl transition-all min-w-[54px]', currentTab === 'users' ? 'text-brand-green-light' : 'text-slate-400 hover:text-white']"
        @click="$emit('change-tab', 'users')"
      >
        <span class="text-xl leading-none">👥</span>
        <span class="text-[11px] font-heading font-semibold">Personal</span>
      </button>

      <!-- 5. Perfil / Cuenta en móvil -->
      <button 
        :class="['flex flex-col items-center gap-1 px-2.5 py-1.5 rounded-xl transition-all min-w-[54px] cursor-pointer', currentTab === 'profile' ? 'text-brand-green-light' : 'text-slate-400 hover:text-white']"
        @click="$emit('change-tab', 'profile')"
        :title="currentUser ? currentUser.fullName : 'Iniciar Sesión / Registrarse'"
      >
        <span class="text-xl leading-none">👤</span>
        <span class="text-[11px] font-heading font-semibold">{{ currentUser ? 'Perfil' : 'Ingresar' }}</span>
      </button>
    </div>
  </div>
</template>
