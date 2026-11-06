<script setup lang="ts">
import { ref, computed } from 'vue';
import authService from '../services/auth';
import api from '../services/api';
import type { User, EmployeeSummary } from '../types';
import {
  User as UserIcon,
  Car,
  ShieldCheck,
  Pencil,
  KeyRound,
  Lock,
  Eye,
  EyeOff,
  CheckCircle2,
  AlertCircle,
  Check
} from 'lucide-vue-next';

const props = defineProps<{
  currentUser?: User | null;
  employeeSummary?: EmployeeSummary;
}>();

const emit = defineEmits<{
  (e: 'update-profile', user: Partial<User>): void;
  (e: 'logout'): void;
  (e: 'notify', message: string, type?: 'success' | 'error' | 'info' | 'warning'): void;
}>();

// Opciones de icono / insignia de perfil
const avatarOptions = ['🚐', '🚚', '🚗', '👑', '👤', '👔', '⚡', '🌟'];

// Estado unificado de edición de perfil (Avatar + Nombre + Teléfono)
const isEditing = ref(false);
const editForm = ref({
  fullName: props.currentUser?.fullName || '',
  phone: props.currentUser?.phone || '',
  avatar: props.currentUser?.avatar || '🚐'
});
const profileLoading = ref(false);

const startEditing = () => {
  editForm.value = {
    fullName: props.currentUser?.fullName || '',
    phone: props.currentUser?.phone || '',
    avatar: props.currentUser?.avatar || (isChofer.value ? '🚐' : '👑')
  };
  isEditing.value = true;
};

const cancelEditing = () => {
  isEditing.value = false;
};

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
  return Boolean(
    passwordForm.value.newPassword &&
    passwordForm.value.newPassword === passwordForm.value.confirmPassword
  );
});
const canSubmitPassword = computed(() => {
  return Boolean(
    passwordForm.value.currentPassword.trim().length > 0 &&
    isNewPasswordLongEnough.value &&
    doPasswordsMatch.value &&
    !passwordLoading.value
  );
});

// Guardar información de perfil (avatar + nombre + teléfono en una sola acción)
const handleSaveProfile = async () => {
  profileLoading.value = true;
  try {
    const updatedUser = {
      ...props.currentUser,
      fullName: editForm.value.fullName.trim(),
      phone: editForm.value.phone.trim(),
      avatar: editForm.value.avatar
    };

    if (props.currentUser?.id) {
      await api.updateUser(props.currentUser.id, updatedUser).catch(() => null);
    }

    localStorage.setItem('tito_auth_user', JSON.stringify(updatedUser));
    localStorage.setItem('auth_user', JSON.stringify(updatedUser));
    localStorage.setItem('tito_current_user', JSON.stringify(updatedUser));

    emit('update-profile', updatedUser);
    emit('notify', '¡Perfil actualizado correctamente!', 'success');
    isEditing.value = false;
  } catch (err: any) {
    emit('notify', err.message || 'Error al actualizar el perfil.', 'error');
  } finally {
    profileLoading.value = false;
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

    passwordForm.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    };
  } catch (err: any) {
    passwordError.value = err.message || 'Error al actualizar la contraseña.';
  } finally {
    passwordLoading.value = false;
  }
};

const formatDate = (dateStr?: string) => {
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
    <div class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-card/95 via-brand-card to-brand-black/95 border border-brand-border p-4 sm:p-6 shadow-card">
      <div class="flex items-center justify-between gap-4 flex-wrap">
        <!-- Avatar estático limpio (sin lápiz suelto que duplique la acción de editar) -->
        <div class="flex items-center gap-3.5 sm:gap-4 min-w-0">
          <div class="w-16 h-16 sm:w-20 sm:h-20 rounded-2xl bg-brand-green/20 border-2 border-brand-green/40 flex items-center justify-center text-brand-green-light shadow-glow shrink-0">
            <Car v-if="isChofer" class="w-8 h-8 sm:w-10 sm:h-10" />
            <ShieldCheck v-else class="w-8 h-8 sm:w-10 sm:h-10" />
          </div>

          <!-- Nombre, Código, Rol y Metadata -->
          <div class="flex flex-col min-w-0">
            <div class="flex items-center gap-2 flex-wrap mb-1">
              <span class="px-2.5 py-0.5 rounded-full font-mono text-xs font-bold bg-brand-green/20 text-brand-green-light border border-brand-green/35">
                {{ currentUser?.code || employeeSummary?.code || 'CHO-001' }}
              </span>
              <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-white/[0.08] border border-white/[0.12] text-slate-200">
                {{ currentUser?.role || 'Chofer' }}
              </span>
              <span class="sm:hidden inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-500/15 border border-emerald-500/30 text-xs font-semibold text-emerald-300">
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
                Cuenta Activa
              </span>
            </div>

            <h1 class="text-xl sm:text-2xl font-extrabold text-white font-heading leading-tight truncate">
              {{ currentUser?.fullName || employeeSummary?.fullName || 'Conductor Tito' }}
            </h1>

            <p class="text-[13px] text-slate-300 mt-1 flex items-center gap-1.5 flex-wrap">
              <span class="text-slate-200 font-medium">{{ currentUser?.department || employeeSummary?.department || 'Transporte Urbano e Interurbano' }}</span>
              <span class="opacity-50">•</span>
              <span>Activo desde {{ formatDate(currentUser?.createdAt) }}</span>
            </p>
          </div>
        </div>

        <!-- Estado de Cuenta (Desktop) -->
        <div class="hidden sm:flex items-center shrink-0">
          <div class="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-brand-green/15 border border-brand-green/30 text-[13px] font-semibold text-brand-green-light select-none">
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
            </span>
            <span>Cuenta Activa</span>
          </div>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- 2. INFORMACIÓN DEL PERFIL Y CONTACTO (ACCIÓN UNIFICADA)  -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-2xl p-4 md:p-5 shadow-sm">
      <div class="flex items-center justify-between mb-4 pb-3 border-b border-brand-border/40">
        <div class="flex items-center gap-2.5">
          <div class="w-8 h-8 rounded-lg bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-brand-green-light">
            <UserIcon class="w-4 h-4" />
          </div>
          <h2 class="text-sm md:text-base font-bold text-white uppercase tracking-wider font-heading">
            Información de Contacto
          </h2>
        </div>

        <!-- Única acción para editar datos y perfil -->
        <button 
          type="button" 
          class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-white/[0.06] hover:bg-white/[0.1] border border-white/[0.12] text-[13px] text-brand-green-light hover:text-white font-semibold transition-all cursor-pointer"
          @click="isEditing ? cancelEditing() : startEditing()"
        >
          <Pencil v-if="!isEditing" class="w-3.5 h-3.5" />
          <span>{{ isEditing ? 'Cancelar' : 'Editar Perfil' }}</span>
        </button>
      </div>

      <!-- Vista de Datos con tipografía y colores consistentes -->
      <div v-if="!isEditing" class="flex flex-col gap-2.5 text-sm">
        <div class="flex items-center justify-between gap-4 p-3 rounded-xl bg-brand-black/40 border border-white/[0.05]">
          <span class="text-slate-300 font-medium">Nombre Completo:</span>
          <span class="font-semibold text-white text-right">{{ currentUser?.fullName }}</span>
        </div>
        <div class="flex items-center justify-between gap-4 p-3 rounded-xl bg-brand-black/40 border border-white/[0.05]">
          <span class="text-slate-300 font-medium">Correo Electrónico:</span>
          <span class="font-semibold text-white text-right break-all">{{ currentUser?.email }}</span>
        </div>
        <div class="flex items-center justify-between gap-4 p-3 rounded-xl bg-brand-black/40 border border-white/[0.05]">
          <span class="text-slate-300 font-medium">Teléfono Móvil:</span>
          <span class="font-semibold text-white text-right">{{ currentUser?.phone || 'No registrado' }}</span>
        </div>
        <div class="flex items-center justify-between gap-4 p-3 rounded-xl bg-brand-black/40 border border-white/[0.05]">
          <span class="text-slate-300 font-medium">Código de Conductor:</span>
          <span class="font-semibold text-white text-right">{{ currentUser?.code || 'CHO-001' }}</span>
        </div>
      </div>

      <!-- Modo Edición Unificado (Nombre + Teléfono + Icono) -->
      <form v-else @submit.prevent="handleSaveProfile" class="flex flex-col gap-4 text-sm">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div>
            <label class="block text-slate-200 mb-1.5 font-semibold text-[13px]">Nombre Completo</label>
            <input 
              v-model="editForm.fullName" 
              type="text" 
              required 
              class="w-full px-3.5 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white text-sm outline-none focus:border-brand-green-light"
            />
          </div>
          <div>
            <label class="block text-slate-200 mb-1.5 font-semibold text-[13px]">Teléfono Móvil</label>
            <input 
              v-model="editForm.phone" 
              type="text" 
              placeholder="+1 (809) 000-0000" 
              class="w-full px-3.5 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white text-sm placeholder-slate-400 outline-none focus:border-brand-green-light"
            />
          </div>
        </div>

        <!-- Selector opcional de insignia dentro del mismo modo de edición -->
        <div>
          <label class="block text-slate-200 mb-1.5 font-semibold text-[13px]">Insignia de Perfil</label>
          <div class="flex items-center gap-2 flex-wrap">
            <button
              v-for="av in avatarOptions"
              :key="av"
              type="button"
              :class="[
                'w-10 h-10 rounded-xl border flex items-center justify-center text-lg transition-all cursor-pointer',
                editForm.avatar === av
                  ? 'bg-brand-green/25 border-brand-green-light scale-105 shadow-sm'
                  : 'bg-brand-charcoal border-brand-border hover:border-brand-border-strong'
              ]"
              @click="editForm.avatar = av"
            >
              {{ av }}
            </button>
          </div>
        </div>

        <div class="flex items-center justify-end gap-2.5 pt-2">
          <button 
            type="button"
            class="px-4 py-2.5 rounded-xl bg-white/[0.06] hover:bg-white/[0.1] text-slate-200 text-[13px] font-semibold cursor-pointer transition-all"
            @click="cancelEditing"
          >
            Cancelar
          </button>
          <button 
            type="submit" 
            :disabled="profileLoading"
            class="px-5 py-2.5 bg-brand-green hover:bg-brand-green-hover text-white text-[13px] font-bold rounded-xl shadow-md shadow-brand-green/30 transition-all cursor-pointer flex items-center justify-center gap-2"
          >
            <span v-if="profileLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <Check v-else class="w-4 h-4" />
            <span>{{ profileLoading ? 'Guardando...' : 'Guardar Cambios' }}</span>
          </button>
        </div>
      </form>
    </div>

    <!-- ======================================================== -->
    <!-- 3. SEGURIDAD: CAMBIO DE CONTRASEÑA CON ÍCONOS SVG        -->
    <!-- ======================================================== -->
    <div class="bg-brand-card border border-brand-border rounded-2xl p-4 md:p-5 shadow-card">
      <div class="flex items-center gap-3 mb-3 pb-3 border-b border-brand-border/40">
        <div class="w-9 h-9 rounded-xl bg-brand-green/15 border border-brand-green/30 flex items-center justify-center text-brand-green-light shrink-0">
          <Lock class="w-4 h-4" />
        </div>
        <div>
          <h2 class="text-sm md:text-base font-bold text-white uppercase tracking-wider font-heading">
            Seguridad y Cambio de Contraseña
          </h2>
          <p class="text-[13px] text-slate-300">
            Actualiza tu clave de acceso personal para ingresar al sistema de Transporte Tito.
          </p>
        </div>
      </div>

      <!-- Alertas de Éxito / Error -->
      <div v-if="passwordSuccess" class="mb-4 p-3.5 rounded-xl bg-emerald-500/15 border border-emerald-500/35 text-emerald-200 text-sm flex items-center gap-2.5 animate-fadeIn">
        <CheckCircle2 class="w-5 h-5 text-emerald-400 shrink-0" />
        <span>{{ passwordSuccess }}</span>
      </div>

      <div v-if="passwordError" class="mb-4 p-3.5 rounded-xl bg-red-500/15 border border-red-500/35 text-red-200 text-sm flex items-center gap-2.5 animate-fadeIn">
        <AlertCircle class="w-5 h-5 text-red-400 shrink-0" />
        <span>{{ passwordError }}</span>
      </div>

      <!-- Formulario de Cambio de Contraseña -->
      <form @submit.prevent="handleChangePassword" class="flex flex-col gap-4 max-w-lg mt-2 text-sm">
        <!-- 1. Contraseña Actual -->
        <div>
          <label class="block text-slate-200 mb-1.5 font-semibold text-[13px]" for="current-pwd">Contraseña Actual</label>
          <div class="relative flex items-center">
            <KeyRound class="w-4 h-4 text-slate-300 absolute left-3.5 pointer-events-none" />
            <input 
              id="current-pwd"
              v-model="passwordForm.currentPassword"
              :type="showCurrentPassword ? 'text' : 'password'"
              required
              placeholder="Ingresa tu contraseña actual"
              class="w-full pl-10 pr-11 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white text-sm placeholder-slate-400 outline-none focus:border-brand-green-light transition-all"
            />
            <button 
              type="button" 
              class="absolute right-3 p-1 text-slate-300 hover:text-white cursor-pointer select-none rounded-lg transition-colors"
              @click="showCurrentPassword = !showCurrentPassword"
              :aria-label="showCurrentPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
            >
              <EyeOff v-if="showCurrentPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
        </div>

        <!-- 2. Nueva Contraseña -->
        <div>
          <label class="block text-slate-200 mb-1.5 font-semibold text-[13px]" for="new-pwd">Nueva Contraseña</label>
          <div class="relative flex items-center">
            <Lock class="w-4 h-4 text-slate-300 absolute left-3.5 pointer-events-none" />
            <input 
              id="new-pwd"
              v-model="passwordForm.newPassword"
              :type="showNewPassword ? 'text' : 'password'"
              required
              placeholder="Mínimo 6 caracteres"
              class="w-full pl-10 pr-11 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white text-sm placeholder-slate-400 outline-none focus:border-brand-green-light transition-all"
            />
            <button 
              type="button" 
              class="absolute right-3 p-1 text-slate-300 hover:text-white cursor-pointer select-none rounded-lg transition-colors"
              @click="showNewPassword = !showNewPassword"
              :aria-label="showNewPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
            >
              <EyeOff v-if="showNewPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
          <div class="flex items-center gap-2 mt-1.5 text-xs">
            <span :class="isNewPasswordLongEnough ? 'text-emerald-400 font-semibold' : 'text-slate-300'">
              {{ isNewPasswordLongEnough ? '✓ Al menos 6 caracteres' : '• Debe tener al menos 6 caracteres' }}
            </span>
          </div>
        </div>

        <!-- 3. Confirmar Nueva Contraseña -->
        <div>
          <label class="block text-slate-200 mb-1.5 font-semibold text-[13px]" for="confirm-pwd">Confirmar Nueva Contraseña</label>
          <div class="relative flex items-center">
            <Lock class="w-4 h-4 text-slate-300 absolute left-3.5 pointer-events-none" />
            <input 
              id="confirm-pwd"
              v-model="passwordForm.confirmPassword"
              :type="showConfirmPassword ? 'text' : 'password'"
              required
              placeholder="Vuelve a escribir la nueva contraseña"
              class="w-full pl-10 pr-11 py-2.5 bg-brand-charcoal border border-brand-border rounded-xl text-white text-sm placeholder-slate-400 outline-none focus:border-brand-green-light transition-all"
            />
            <button 
              type="button" 
              class="absolute right-3 p-1 text-slate-300 hover:text-white cursor-pointer select-none rounded-lg transition-colors"
              @click="showConfirmPassword = !showConfirmPassword"
              :aria-label="showConfirmPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
            >
              <EyeOff v-if="showConfirmPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
          <div v-if="passwordForm.confirmPassword" class="flex items-center gap-2 mt-1.5 text-xs">
            <span :class="doPasswordsMatch ? 'text-emerald-400 font-semibold' : 'text-amber-300 font-semibold'">
              {{ doPasswordsMatch ? '✓ Las contraseñas coinciden' : '✕ Las contraseñas no coinciden' }}
            </span>
          </div>
        </div>

        <!-- Botón de Envío con contraste diferenciado entre habilitado y deshabilitado -->
        <div class="pt-2">
          <button 
            type="submit" 
            :disabled="!canSubmitPassword"
            :class="[
              'px-5 py-2.5 font-heading font-bold rounded-xl text-[13px] md:text-sm transition-all flex items-center gap-2',
              canSubmitPassword
                ? 'bg-gradient-to-r from-brand-green-light to-brand-green hover:from-emerald-400 hover:to-brand-green-hover text-brand-black shadow-lg shadow-brand-green/35 cursor-pointer'
                : 'bg-white/[0.06] border border-white/[0.12] text-slate-400 cursor-not-allowed'
            ]"
          >
            <span v-if="passwordLoading" class="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin"></span>
            <Check v-else class="w-4 h-4" />
            <span>{{ passwordLoading ? 'Actualizando contraseña...' : 'Actualizar Contraseña' }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
