<script setup lang="ts">
import { ref, computed } from 'vue';
import authService from '../services/auth';
import api from '../services/api';
import type { User, EmployeeSummary } from '../types';

const props = defineProps<{
  currentUser?: User | null;
  employeeSummary?: EmployeeSummary;
}>();

const emit = defineEmits<{
  (e: 'update-profile', user: Partial<User>): void;
  (e: 'logout'): void;
  (e: 'notify', message: string, type?: 'success' | 'error' | 'info' | 'warning'): void;
}>();

// Opciones de avatar
const avatarOptions = ['🚐', '🚚', '🚗', '👑', '👤', '👔', '⚡', '🌟'];
const showAvatarPicker = ref(false);

// Estado de edición de datos de contacto
const isEditing = ref(false);
const editForm = ref({
  fullName: props.currentUser?.fullName || '',
  phone: props.currentUser?.phone || '',
  avatar: props.currentUser?.avatar || '🚐'
});
const profileLoading = ref(false);

// Formulario de cambio de contraseña
const passwordForm = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
});
const showCurrentPassword = ref(false);
const showNewPassword = ref(false);
const showConfirmPassword = ref(false);
const passwordLoading = ref(false);
const passwordSuccess = ref('');
const passwordError = ref('');

// Determinar rol
const isChofer = computed(() => {
  return props.currentUser?.role === 'Chofer' || !props.currentUser?.isSuperAdmin;
});

// Validaciones de contraseña
const isNewPasswordLongEnough = computed(() => passwordForm.value.newPassword.length >= 6);
const doPasswordsMatch = computed(() => {
  return passwordForm.value.newPassword && 
         passwordForm.value.newPassword === passwordForm.value.confirmPassword;
});

// Guardar información de perfil
const handleSaveProfile = async () => {
  profileLoading.value = true;
  try {
    const updatedUser = {
      ...props.currentUser,
      fullName: editForm.value.fullName,
      phone: editForm.value.phone,
      avatar: editForm.value.avatar
    };

    // Intentar actualizar en backend si existe el ID
    if (props.currentUser?.id) {
      await api.updateUser(props.currentUser.id, updatedUser).catch(() => null);
    }

    // Actualizar localmente en almacenamiento
    localStorage.setItem('auth_user', JSON.stringify(updatedUser));
    localStorage.setItem('tito_current_user', JSON.stringify(updatedUser));

    emit('update-profile', updatedUser);
    emit('notify', '¡Perfil actualizado correctamente!', 'success');
    isEditing.value = false;
  } catch (err) {
    emit('notify', err.message || 'Error al actualizar el perfil.', 'error');
  } finally {
    profileLoading.value = false;
  }
};

const selectAvatar = (av) => {
  editForm.value.avatar = av;
  showAvatarPicker.value = false;
  if (!isEditing.value) {
    handleSaveProfile();
  }
};

// Manejo de cambio de contraseña
const handleChangePassword = async () => {
  passwordError.value = '';
  passwordSuccess.value = '';

  if (!passwordForm.value.currentPassword) {
    passwordError.value = 'Por favor ingresa tu contraseña actual.';
    return;
  }

  if (!isNewPasswordLongEnough.value) {
    passwordError.value = 'La nueva contraseña debe tener al menos 6 caracteres.';
    return;
  }

  if (!doPasswordsMatch.value) {
    passwordError.value = 'La confirmación de la contraseña no coincide.';
    return;
  }

  try {
    passwordLoading.value = true;
    const res = await authService.changePassword(
      passwordForm.value.currentPassword,
      passwordForm.value.newPassword
    );

    passwordSuccess.value = res.message || '¡Tu contraseña ha sido actualizada con éxito!';
    emit('notify', '¡Contraseña actualizada con éxito!', 'success');

    // Limpiar formulario
    passwordForm.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    };
  } catch (err) {
    passwordError.value = err.message || 'Error al actualizar la contraseña.';
  } finally {
    passwordLoading.value = false;
  }
};

const formatDate = (dateStr) => {
  if (!dateStr) return 'Enero 2026';
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'long',
    year: 'numeric'
  });
};
</script>

<template>
  <div class="flex flex-col gap-5 max-w-4xl mx-auto animate-fadeIn">
    <!-- ======================================================== -->
    <!-- 1. HERO / CABECERA DE PERFIL DEL USUARIO                 -->
    <!-- ======================================================== -->
    <div class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-card/95 via-brand-card to-brand-black/95 border border-brand-border p-5 md:p-6 shadow-card">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <!-- Avatar y Datos Principales -->
        <div class="flex items-center gap-4">
          <!-- Selector de Avatar Interactivo -->
          <div class="relative">
            <button 
              type="button"
              class="w-16 h-16 md:w-20 md:h-20 rounded-2xl bg-brand-green/20 border-2 border-brand-green/40 hover:border-brand-green-light flex items-center justify-center text-3xl md:text-4xl shadow-glow cursor-pointer transition-all hover:scale-105 active:scale-95"
              @click="showAvatarPicker = !showAvatarPicker"
              title="Cambiar avatar"
            >
              <span>{{ editForm.avatar || currentUser?.avatar || (isChofer ? '🚐' : '👑') }}</span>
              <span class="absolute -bottom-1 -right-1 bg-brand-charcoal border border-brand-border text-white text-[10px] w-5 h-5 rounded-full flex items-center justify-center">
                ✏️
              </span>
            </button>

            <!-- Desplegable selector de Avatar -->
            <div 
              v-if="showAvatarPicker" 
              class="absolute top-full left-0 mt-2 bg-brand-charcoal border border-brand-border-strong rounded-xl p-2.5 shadow-elevated z-50 flex gap-2"
            >
              <button 
                v-for="av in avatarOptions" 
                :key="av" 
                class="w-9 h-9 rounded-lg hover:bg-white/10 flex items-center justify-center text-xl transition-all cursor-pointer"
                @click="selectAvatar(av)"
              >
                {{ av }}
              </button>
            </div>
          </div>

          <!-- Nombre, Código y Rol -->
          <div class="flex flex-col">
            <div class="flex items-center gap-2 flex-wrap mb-1">
              <span class="px-2 py-0.5 rounded-full font-mono text-[10px] font-bold bg-brand-green/20 text-brand-green-light border border-brand-green/30">
                {{ currentUser?.code || employeeSummary?.code || 'CHO-001' }}
              </span>
              <span class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-white/[0.06] border border-white/[0.1] text-slate-300">
                {{ currentUser?.role || 'Chofer' }}
              </span>
              <span v-if="currentUser?.commissionRate || employeeSummary?.commissionRate" class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/15 text-emerald-300 border border-emerald-500/30">
                Comisión: {{ currentUser?.commissionRate || employeeSummary?.commissionRate || 25 }}%
              </span>
            </div>

            <h1 class="text-xl md:text-2xl font-extrabold text-white font-heading leading-tight">
              {{ currentUser?.fullName || employeeSummary?.fullName || 'Conductor Tito' }}
            </h1>

            <p class="text-xs text-slate-400 mt-0.5 flex items-center gap-1.5 flex-wrap">
              <span>{{ currentUser?.department || employeeSummary?.department || 'Transporte Urbano e Interurbano' }}</span>
              <span class="opacity-40">•</span>
              <span>Activo desde {{ formatDate(currentUser?.createdAt) }}</span>
            </p>
          </div>
        </div>

        <!-- Estado de Cuenta y Botón Cerrar Sesión -->
        <div class="flex items-center gap-2.5">
          <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-brand-green/10 border border-brand-green/25 text-xs font-semibold text-brand-green-light select-none">
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
            </span>
            <span>Cuenta Activa</span>
          </div>

          <button 
            type="button"
            class="px-3.5 py-1.5 rounded-xl bg-red-500/10 hover:bg-red-500/20 border border-red-500/30 text-red-300 hover:text-red-200 text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer"
            @click="$emit('logout')"
            title="Cerrar sesión"
          >
            <span>🚪</span>
            <span class="hidden sm:inline">Cerrar Sesión</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 2. INFORMACIÓN DEL CHOFER Y DETALLES OPERATIVOS          -->
    <!-- ======================================================== -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <!-- Tarjeta 1: Información Personal y de Contacto -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-4 md:p-5 shadow-sm flex flex-col justify-between">
        <div>
          <div class="flex items-center justify-between mb-4 pb-2 border-b border-brand-border/40">
            <div class="flex items-center gap-2">
              <span class="text-lg">📋</span>
              <h3 class="text-sm font-bold text-white uppercase tracking-wider">Información de Contacto</h3>
            </div>
            <button 
              type="button" 
              class="text-xs text-brand-green-light hover:underline font-semibold cursor-pointer"
              @click="isEditing = !isEditing"
            >
              {{ isEditing ? 'Cancelar' : 'Editar Datos' }}
            </button>
          </div>

          <!-- Vista de Datos o Formulario de Edición -->
          <div v-if="!isEditing" class="flex flex-col gap-3 text-xs">
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Nombre Completo:</span>
              <span class="font-bold text-white">{{ currentUser?.fullName }}</span>
            </div>
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Correo Electrónico:</span>
              <span class="font-mono text-slate-200">{{ currentUser?.email }}</span>
            </div>
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Teléfono Móvil:</span>
              <span class="font-bold text-brand-green-light">{{ currentUser?.phone || 'No registrado' }}</span>
            </div>
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Código de Conductor:</span>
              <span class="font-mono font-bold text-slate-200">{{ currentUser?.code || 'CHO-001' }}</span>
            </div>
          </div>

          <!-- Modo Edición Rápida de Contacto -->
          <form v-else @submit.prevent="handleSaveProfile" class="flex flex-col gap-3 text-xs">
            <div>
              <label class="block text-slate-400 mb-1 font-semibold">Nombre Completo</label>
              <input 
                v-model="editForm.fullName" 
                type="text" 
                required 
                class="w-full px-3 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-white outline-none focus:border-brand-green-light"
              />
            </div>
            <div>
              <label class="block text-slate-400 mb-1 font-semibold">Teléfono Celular</label>
              <input 
                v-model="editForm.phone" 
                type="text" 
                placeholder="+1 (809) 000-0000" 
                class="w-full px-3 py-2 bg-brand-charcoal border border-brand-border rounded-lg text-white outline-none focus:border-brand-green-light"
              />
            </div>
            <button 
              type="submit" 
              :disabled="profileLoading"
              class="mt-2 w-full py-2 bg-brand-green hover:bg-brand-green-hover text-white font-semibold rounded-lg shadow-md transition-all cursor-pointer flex items-center justify-center gap-2"
            >
              <span v-if="profileLoading" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>{{ profileLoading ? 'Guardando...' : 'Guardar Cambios' }}</span>
            </button>
          </form>
        </div>
      </div>

      <!-- Tarjeta 2: Condiciones Operativas y Beneficios -->
      <div class="bg-brand-card border border-brand-border rounded-xl p-4 md:p-5 shadow-sm flex flex-col justify-between">
        <div>
          <div class="flex items-center gap-2 mb-4 pb-2 border-b border-brand-border/40">
            <span class="text-lg">💼</span>
            <h3 class="text-sm font-bold text-white uppercase tracking-wider">Condiciones & Asignación</h3>
          </div>

          <div class="flex flex-col gap-3 text-xs">
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Tasa de Comisión por Viaje:</span>
              <span class="font-bold text-brand-green-light text-sm">{{ currentUser?.commissionRate || employeeSummary?.commissionRate || 25 }}%</span>
            </div>
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Incentivo Flota / Celular:</span>
              <span class="font-bold text-slate-200">RD$ 1,000.00 / mes</span>
            </div>
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Régimen de Nómina:</span>
              <span class="text-slate-300 font-medium">Consolidado Mensual</span>
            </div>
            <div class="flex items-center justify-between p-2 rounded-lg bg-brand-black/30">
              <span class="text-slate-400">Deducciones Estatutarias:</span>
              <span class="text-slate-400 text-[11px]">TSS (Ley 87-01) & Retención ISR</span>
            </div>
          </div>
        </div>

        <p class="text-[11px] text-slate-500 mt-4 italic">
          💡 La tasa de comisión y los beneficios fijos son administrados por la Dirección General de Transporte Tito.
        </p>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 3. SEGURIDAD: CAMBIO DE CONTRASEÑA                       -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-xl p-4 md:p-5 shadow-card">
      <div class="flex items-center gap-2.5 mb-2 pb-2 border-b border-brand-border/40">
        <span class="text-xl">🔐</span>
        <div>
          <h3 class="text-sm md:text-base font-bold text-white uppercase tracking-wider font-heading">
            Seguridad y Cambio de Contraseña
          </h3>
          <p class="text-xs text-slate-400">
            Actualiza tu clave de acceso personal para ingresar al sistema de Transporte Tito.
          </p>
        </div>
      </div>

      <!-- Alertas de Éxito / Error -->
      <div v-if="passwordSuccess" class="mb-4 p-3 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-300 text-xs flex items-center gap-2 animate-fadeIn">
        <span class="text-base">✅</span>
        <span>{{ passwordSuccess }}</span>
      </div>

      <div v-if="passwordError" class="mb-4 p-3 rounded-xl bg-red-500/15 border border-red-500/30 text-red-300 text-xs flex items-center gap-2 animate-fadeIn">
        <span class="text-base">⚠️</span>
        <span>{{ passwordError }}</span>
      </div>

      <!-- Formulario de Cambio de Contraseña -->
      <form @submit.prevent="handleChangePassword" class="flex flex-col gap-3.5 max-w-lg mt-2 text-xs">
        <!-- 1. Contraseña Actual -->
        <div>
          <label class="block text-slate-300 mb-1 font-semibold" for="current-pwd">Contraseña Actual</label>
          <div class="relative flex items-center">
            <span class="absolute left-3 text-slate-500 text-sm">🔑</span>
            <input 
              id="current-pwd"
              v-model="passwordForm.currentPassword"
              :type="showCurrentPassword ? 'text' : 'password'"
              required
              placeholder="Ingresa tu contraseña actual"
              class="w-full pl-9 pr-10 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white placeholder-slate-500 outline-none focus:border-brand-green-light focus:ring-1 focus:ring-brand-green-light transition-all"
            />
            <button 
              type="button" 
              class="absolute right-3 text-slate-400 hover:text-white cursor-pointer select-none text-sm"
              @click="showCurrentPassword = !showCurrentPassword"
              tabindex="-1"
            >
              {{ showCurrentPassword ? '👁️' : '🙈' }}
            </button>
          </div>
        </div>

        <!-- 2. Nueva Contraseña -->
        <div>
          <label class="block text-slate-300 mb-1 font-semibold" for="new-pwd">Nueva Contraseña</label>
          <div class="relative flex items-center">
            <span class="absolute left-3 text-slate-500 text-sm">🔒</span>
            <input 
              id="new-pwd"
              v-model="passwordForm.newPassword"
              :type="showNewPassword ? 'text' : 'password'"
              required
              placeholder="Mínimo 6 caracteres"
              class="w-full pl-9 pr-10 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white placeholder-slate-500 outline-none focus:border-brand-green-light focus:ring-1 focus:ring-brand-green-light transition-all"
            />
            <button 
              type="button" 
              class="absolute right-3 text-slate-400 hover:text-white cursor-pointer select-none text-sm"
              @click="showNewPassword = !showNewPassword"
              tabindex="-1"
            >
              {{ showNewPassword ? '👁️' : '🙈' }}
            </button>
          </div>
          <div class="flex items-center gap-2 mt-1 text-[11px]">
            <span :class="isNewPasswordLongEnough ? 'text-emerald-400 font-medium' : 'text-slate-500'">
              {{ isNewPasswordLongEnough ? '✓' : '•' }} Al menos 6 caracteres
            </span>
          </div>
        </div>

        <!-- 3. Confirmar Nueva Contraseña -->
        <div>
          <label class="block text-slate-300 mb-1 font-semibold" for="confirm-pwd">Confirmar Nueva Contraseña</label>
          <div class="relative flex items-center">
            <span class="absolute left-3 text-slate-500 text-sm">🔒</span>
            <input 
              id="confirm-pwd"
              v-model="passwordForm.confirmPassword"
              :type="showConfirmPassword ? 'text' : 'password'"
              required
              placeholder="Vuelve a escribir la nueva contraseña"
              class="w-full pl-9 pr-10 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white placeholder-slate-500 outline-none focus:border-brand-green-light focus:ring-1 focus:ring-brand-green-light transition-all"
            />
            <button 
              type="button" 
              class="absolute right-3 text-slate-400 hover:text-white cursor-pointer select-none text-sm"
              @click="showConfirmPassword = !showConfirmPassword"
              tabindex="-1"
            >
              {{ showConfirmPassword ? '👁️' : '🙈' }}
            </button>
          </div>
          <div v-if="passwordForm.confirmPassword" class="flex items-center gap-2 mt-1 text-[11px]">
            <span :class="doPasswordsMatch ? 'text-emerald-400 font-medium' : 'text-amber-400 font-medium'">
              {{ doPasswordsMatch ? '✓ Las contraseñas coinciden' : '✕ Las contraseñas no coinciden' }}
            </span>
          </div>
        </div>

        <!-- Botón de Envío -->
        <div class="pt-2">
          <button 
            type="submit" 
            :disabled="passwordLoading || !isNewPasswordLongEnough || !doPasswordsMatch"
            class="px-5 py-2.5 bg-gradient-to-r from-brand-green-hover to-brand-green hover:from-brand-green-light hover:to-brand-green-hover disabled:opacity-50 disabled:cursor-not-allowed text-white font-heading font-semibold rounded-xl text-xs shadow-md shadow-brand-green/30 transition-all flex items-center gap-2 cursor-pointer"
          >
            <span v-if="passwordLoading" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ passwordLoading ? 'Actualizando contraseña...' : 'Actualizar Contraseña' }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
