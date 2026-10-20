<script setup lang="ts">
import { ref, watch, computed, onMounted, onUnmounted } from 'vue';
import type { User } from '../types';

const props = defineProps<{
  show: boolean;
  user?: User | null;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'submit', payload: Partial<User>): void;
}>();

const isEditing = computed(() => !!props.user);

const form = ref({
  fullName: '',
  email: '',
  password: '', // Solo usado en edición opcional
  phone: '',
  role: 'Chofer',
  department: 'Rutas Interurbanas',
  commissionRate: 25,
  isActive: true
});

const showPassword = ref(false);
const errorMessage = ref('');

// Cerrar modal al presionar tecla Escape
const onKeyDown = (e: KeyboardEvent) => {
  if (e.key === 'Escape' && props.show) {
    emit('close');
  }
};

onMounted(() => {
  window.addEventListener('keydown', onKeyDown);
});

onUnmounted(() => {
  window.removeEventListener('keydown', onKeyDown);
});

// Sincronizar formulario al abrir o cambiar usuario
watch(
  () => props.user,
  (newVal) => {
    if (newVal) {
      form.value = {
        fullName: newVal.fullName || '',
        email: newVal.email || '',
        password: '', // En edición es opcional
        phone: newVal.phone || '',
        role: newVal.role || 'Chofer',
        department: newVal.department || (newVal.role === 'Chofer' ? 'Rutas Interurbanas' : 'Operaciones & Logística'),
        commissionRate: newVal.commissionRate ?? (newVal.role === 'Chofer' ? 25 : 0),
        isActive: newVal.isActive !== false
      };
    } else {
      // Valores por defecto para nuevo usuario
      form.value = {
        fullName: '',
        email: '',
        password: '',
        phone: '',
        role: 'Chofer',
        department: 'Rutas Interurbanas',
        commissionRate: 25,
        isActive: true
      };
    }
    errorMessage.value = '';
    showPassword.value = false;
  },
  { immediate: true }
);

// Ajustar departamento y comisión automáticamente al cambiar rol
const onRoleChange = () => {
  if (form.value.role === 'Chofer') {
    if (form.value.commissionRate === 0) form.value.commissionRate = 25;
    if (!form.value.department || form.value.department.includes('Operaciones') || form.value.department.includes('Dirección')) {
      form.value.department = 'Rutas Interurbanas';
    }
  } else {
    form.value.commissionRate = 0;
    if (!form.value.department || form.value.department.includes('Rutas')) {
      form.value.department = form.value.role === 'SuperAdmin' ? 'Dirección General' : 'Operaciones & Logística';
    }
  }
};

const handleSubmit = () => {
  errorMessage.value = '';

  const trimmedName = form.value.fullName.trim();
  if (!trimmedName || trimmedName.length < 3) {
    errorMessage.value = 'El nombre completo debe tener al menos 3 caracteres.';
    return;
  }

  // Prevenir que se escriba un correo en el campo de Nombre
  if (trimmedName.includes('@')) {
    errorMessage.value = 'El campo "Nombre y Apellido" no debe contener un correo electrónico.';
    return;
  }

  const cleanEmail = form.value.email.trim().toLowerCase();
  if (!cleanEmail || !cleanEmail.includes('@') || !cleanEmail.includes('.')) {
    errorMessage.value = 'Por favor ingresa un correo electrónico válido.';
    return;
  }

  // En edición, si escribió contraseña debe tener al menos 6 caracteres
  if (isEditing.value && form.value.password && form.value.password.length < 6) {
    errorMessage.value = 'La nueva contraseña debe tener al menos 6 caracteres.';
    return;
  }

  // Validación de comisión para chofer
  if (form.value.role === 'Chofer') {
    const rate = Number(form.value.commissionRate);
    if (isNaN(rate) || rate < 0 || rate > 100) {
      errorMessage.value = 'La comisión debe ser un valor porcentual entre 0% y 100%.';
      return;
    }
  }

  const payload: Partial<User> & { password?: string } = {
    fullName: trimmedName,
    email: cleanEmail,
    phone: form.value.phone ? form.value.phone.trim() : '',
    role: form.value.role,
    department: form.value.department ? form.value.department.trim() : (form.value.role === 'Chofer' ? 'Rutas Interurbanas' : 'Operaciones & Logística'),
    commissionRate: form.value.role === 'Chofer' ? Number(form.value.commissionRate) || 0 : 0,
    isActive: form.value.isActive,
    avatar: form.value.role === 'Chofer' ? '🚚' : (form.value.role === 'SuperAdmin' ? '👑' : '👔')
  };

  // Solo enviar password en edición si el administrador lo especificó
  if (isEditing.value && form.value.password) {
    payload.password = form.value.password;
  }

  emit('submit', payload);
};
</script>

<template>
  <transition name="modal-fade">
    <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
      <div class="modal-card" role="dialog" aria-modal="true" :aria-label="isEditing ? 'Editar Usuario' : 'Crear Nuevo Usuario'">
        <!-- Encabezado del modal -->
        <div class="modal-header">
          <div class="header-icon">
            <!-- Icono SVG según el rol -->
            <svg v-if="form.role === 'Chofer'" class="w-6 h-6 text-brand-green-light" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"/>
              <path d="M15 18H9"/>
              <path d="M19 18h2a1 1 0 0 0 1-1v-5l-3-4h-5v10"/>
              <circle cx="7" cy="18" r="2"/>
              <circle cx="17" cy="18" r="2"/>
            </svg>
            <svg v-else-if="form.role === 'SuperAdmin'" class="w-6 h-6 text-amber-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="m2 4 3 12h14l3-12-6 7-4-7-4 7-6-7zm3 16h14"/>
            </svg>
            <svg v-else class="w-6 h-6 text-blue-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            </svg>
          </div>
          <div class="flex-1">
            <h3 class="modal-title">{{ isEditing ? 'Editar Usuario' : 'Crear Nuevo Usuario' }}</h3>
            <p class="modal-subtitle">
              {{ isEditing ? `Modificando credenciales y perfil de ${props.user?.fullName}` : 'Registra un Administrador o Chofer para la flota de Transporte Tito' }}
            </p>
          </div>
          <button class="modal-close" @click="$emit('close')" aria-label="Cerrar modal">&times;</button>
        </div>

        <!-- Banner de Error -->
        <transition name="fade">
          <div v-if="errorMessage" class="error-banner">
            <svg class="w-5 h-5 shrink-0 text-red-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/>
              <line x1="12" y1="9" x2="12" y2="13"/>
              <line x1="12" y1="17" x2="12.01" y2="17"/>
            </svg>
            <span>{{ errorMessage }}</span>
          </div>
        </transition>

        <!-- Formulario Responsive a 2 Columnas -->
        <form @submit.prevent="handleSubmit" class="modal-form" autocomplete="off">
          <!-- FILA 1: Rol en la empresa (Selector destacado) -->
          <div class="form-group">
            <label class="form-label" for="user-role">Rol en la Empresa *</label>
            <div class="input-wrapper">
              <span class="input-icon">
                <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/>
                  <circle cx="9" cy="7" r="4"/>
                  <path d="M22 21v-2a4 4 0 0 0-3-3.87"/>
                  <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                </svg>
              </span>
              <select 
                id="user-role" 
                v-model="form.role" 
                class="form-select with-icon" 
                @change="onRoleChange"
                autocomplete="off"
              >
                <option value="Chofer">Chofer / Conductor de Flota</option>
                <option value="Administrador">Administrador de Operaciones</option>
                <option value="SuperAdmin">Super Administrador (Dirección)</option>
              </select>
            </div>
          </div>

          <!-- FILA 2: Nombre Completo y Correo Electrónico (2 Columnas) -->
          <div class="form-grid-2">
            <!-- Nombre Completo -->
            <div class="form-group">
              <label class="form-label" for="user-name">Nombre y Apellido *</label>
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                    <circle cx="12" cy="7" r="4"/>
                  </svg>
                </span>
                <input 
                  id="user-name" 
                  v-model="form.fullName" 
                  type="text" 
                  class="form-input with-icon" 
                  placeholder="ej. Carlos Rosario" 
                  autocomplete="name"
                  required 
                />
              </div>
            </div>

            <!-- Correo Electrónico -->
            <div class="form-group">
              <label class="form-label" for="user-email">Correo Electrónico *</label>
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect width="20" height="16" x="2" y="4" rx="2"/>
                    <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
                  </svg>
                </span>
                <input 
                  id="user-email" 
                  v-model="form.email" 
                  type="email" 
                  class="form-input with-icon" 
                  placeholder="carlos.rosario@titotransporte.com" 
                  autocomplete="email"
                  required 
                  :disabled="isEditing"
                />
              </div>
              <span v-if="isEditing" class="input-hint">El correo identifica al usuario y no se puede modificar.</span>
            </div>
          </div>

          <!-- FILA 3: Teléfono y Campo según Rol (Ruta para chofer / Depto para admin) -->
          <div class="form-grid-2">
            <!-- Teléfono / Celular -->
            <div class="form-group">
              <label class="form-label" for="user-phone">Teléfono / WhatsApp</label>
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
                  </svg>
                </span>
                <input 
                  id="user-phone" 
                  v-model="form.phone" 
                  type="tel" 
                  class="form-input with-icon" 
                  placeholder="809-555-0303" 
                  autocomplete="tel"
                />
              </div>
            </div>

            <!-- Campo Específico por Rol: Ruta para Chofer, Departamento para Admin -->
            <div class="form-group">
              <label class="form-label" for="user-dept">
                {{ form.role === 'Chofer' ? 'Ruta Habitual / Asignada *' : 'Departamento / Área *' }}
              </label>
              <div class="input-wrapper">
                <span class="input-icon">
                  <!-- Ruta icon -->
                  <svg v-if="form.role === 'Chofer'" class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="6" cy="19" r="3"/>
                    <path d="M9 19h8.5a3.5 3.5 0 0 0 0-7h-11a3.5 3.5 0 0 1 0-7H15"/>
                    <circle cx="18" cy="5" r="3"/>
                  </svg>
                  <!-- Edificio icon -->
                  <svg v-else class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect width="16" height="20" x="4" y="2" rx="2"/>
                    <path d="M9 22v-4h6v4"/>
                    <path d="M8 6h.01"/><path d="M16 6h.01"/>
                    <path d="M8 10h.01"/><path d="M16 10h.01"/>
                  </svg>
                </span>
                <input 
                  id="user-dept" 
                  v-model="form.department" 
                  type="text" 
                  class="form-input with-icon" 
                  :placeholder="form.role === 'Chofer' ? 'ej. Rutas Interurbanas / Cibao' : 'ej. Operaciones & Logística'" 
                  required
                />
              </div>
            </div>
          </div>

          <!-- FILA 4 (Solo Choferes): Tasa de Comisión con Sufijo % y validación 0-100 -->
          <div v-if="form.role === 'Chofer'" class="form-group">
            <label class="form-label" for="user-commission">Tasa de Comisión por Viaje *</label>
            <div class="input-wrapper">
              <span class="input-icon">
                <svg class="w-4 h-4 text-brand-green-light" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="12" y1="1" x2="12" y2="23"/>
                  <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
                </svg>
              </span>
              <input 
                id="user-commission" 
                v-model.number="form.commissionRate" 
                type="number" 
                min="0" 
                max="100" 
                step="0.5" 
                class="form-input with-icon pr-10 font-mono font-bold text-brand-green-light" 
                placeholder="25.0" 
                required
              />
              <span class="commission-suffix">%</span>
            </div>
            <span class="input-hint">Porcentaje de ganancia directa que acumulará el chofer en cada viaje completado (ej. 25%).</span>
          </div>

          <!-- FILA 5 (Modo Edición): Restablecer contraseña opcional -->
          <div v-if="isEditing" class="form-group">
            <label class="form-label" for="user-pass">Nueva Contraseña (Opcional)</label>
            <div class="input-wrapper">
              <span class="input-icon">
                <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect width="18" height="11" x="3" y="11" rx="2" ry="2"/>
                  <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </span>
              <input 
                id="user-pass" 
                v-model="form.password" 
                :type="showPassword ? 'text' : 'password'" 
                class="form-input with-icon pr-12" 
                placeholder="Dejar en blanco para mantener la contraseña actual" 
                autocomplete="new-password"
              />
              <!-- Botón Ojo con área táctil accesible de 40px -->
              <button 
                type="button" 
                class="eye-toggle-btn" 
                @click="showPassword = !showPassword"
                :title="showPassword ? 'Ocultar contraseña' : 'Ver contraseña'"
                aria-label="Alternar visibilidad de contraseña"
              >
                <svg v-if="showPassword" class="w-4 h-4 text-slate-300 hover:text-white" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M9.88 9.88a3 3 0 1 0 4.24 4.24"/>
                  <path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68"/>
                  <path d="M6.61 6.61A13.526 13.526 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61"/>
                  <line x1="2" y1="22" x2="22" y2="2"/>
                </svg>
                <svg v-else class="w-4 h-4 text-slate-300 hover:text-white" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
              </button>
            </div>
          </div>

          <!-- FILA 6: Estado Activo (solo en edición) -->
          <div v-if="isEditing" class="form-group">
            <label class="form-label">Estado de la Cuenta</label>
            <label class="checkbox-row">
              <input type="checkbox" v-model="form.isActive" class="custom-checkbox" />
              <span class="text-xs font-semibold text-white">Usuario Activo en el Sistema</span>
            </label>
          </div>

          <!-- AVISO INFORMATIVO DE ONBOARDING: Contraseña automática por código -->
          <div v-if="!isEditing" class="role-hint-box">
            <svg class="w-5 h-5 shrink-0 text-emerald-400 mt-0.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="12" y1="16" x2="12" y2="12"/>
              <line x1="12" y1="8" x2="12.01" y2="8"/>
            </svg>
            <div class="text-xs leading-relaxed text-emerald-200">
              <strong class="text-white block mb-0.5">Contraseña inicial automática:</strong>
              No necesitas crear una contraseña manual. La contraseña temporal será automáticamente el código oficial que asigne el sistema (ej. <code class="code-pill">CHO-007</code> o <code class="code-pill">ADM-002</code>). En su primer inicio de sesión, el sistema le exigirá actualizarla por una contraseña personal privada.
            </div>
          </div>

          <!-- Acciones del Modal -->
          <div class="modal-actions">
            <button type="button" class="btn btn-outline" @click="$emit('close')">
              Cancelar
            </button>
            <button type="submit" class="btn btn-primary" :disabled="loading">
              <span v-if="loading" class="spinner"></span>
              <span v-else>{{ isEditing ? 'Guardar Cambios' : 'Crear Usuario' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 1600;
  background: rgba(6, 10, 8, 0.88);
  backdrop-filter: blur(14px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px 20px;
  overflow-y: auto;
}

.modal-card {
  position: relative;
  width: 100%;
  max-width: 650px;
  max-height: calc(100vh - 48px);
  overflow-y: auto;
  margin: auto;
  background: linear-gradient(180deg, #131b16 0%, #0c120f 100%);
  border: 1px solid rgba(34, 197, 94, 0.3);
  box-shadow: 0 24px 64px rgba(0, 0, 0, 0.9), 0 0 35px rgba(34, 197, 94, 0.12);
  border-radius: var(--radius-lg, 18px);
  padding: 24px 26px;
  color: #ffffff;
  animation: modalScale 0.22s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalScale {
  from { opacity: 0; transform: scale(0.96) translateY(12px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}

.modal-header {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  margin-bottom: 18px;
  position: relative;
}

.header-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: rgba(34, 197, 94, 0.12);
  border: 1px solid rgba(34, 197, 94, 0.28);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 800;
  color: #ffffff;
  font-family: var(--font-heading);
  letter-spacing: -0.02em;
  line-height: 1.2;
}

.modal-subtitle {
  font-size: 0.82rem;
  color: #94a3b8;
  margin-top: 3px;
  line-height: 1.35;
}

.modal-close {
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 1.8rem;
  line-height: 1;
  cursor: pointer;
  padding: 4px;
  border-radius: 8px;
  transition: all 0.15s ease;
}

.modal-close:hover {
  color: #ffffff;
  background: rgba(255, 255, 255, 0.08);
}

.error-banner {
  background: rgba(239, 68, 68, 0.14);
  border: 1px solid rgba(239, 68, 68, 0.4);
  color: #fca5a5;
  padding: 10px 14px;
  border-radius: 10px;
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 0.85rem;
}

.modal-form {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.form-grid-2 {
  display: grid;
  grid-template-columns: 1fr;
  gap: 14px;
}

@media (min-width: 560px) {
  .form-grid-2 {
    grid-template-columns: 1fr 1fr;
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-label {
  font-size: 0.76rem;
  font-weight: 700;
  color: #cbd5e1; /* Alto contraste slate-300 */
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-icon {
  position: absolute;
  left: 13px;
  display: flex;
  align-items: center;
  justify-content: center;
  pointer-events: none;
  z-index: 2;
}

.form-input, .form-select {
  width: 100%;
  padding: 10px 14px;
  background: #0f1612;
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 10px;
  color: #ffffff;
  font-size: 0.9rem;
  font-family: inherit;
  transition: all 0.18s ease;
}

.form-input::placeholder {
  color: #64748b; /* slate-500: claramente diferenciado de texto ingresado */
  opacity: 1;
}

.form-input.with-icon, .form-select.with-icon {
  padding-left: 38px;
}

.form-input:disabled {
  opacity: 0.55;
  cursor: not-allowed;
  background: #141c17;
  border-color: rgba(255, 255, 255, 0.08);
}

.form-input:focus, .form-select:focus {
  outline: none;
  border-color: #22c55e;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.22);
}

/* Override profundo de autofill en Chrome/Webkit */
input:-webkit-autofill,
input:-webkit-autofill:hover,
input:-webkit-autofill:focus,
select:-webkit-autofill {
  -webkit-text-fill-color: #ffffff !important;
  -webkit-box-shadow: 0 0 0px 1000px #0f1612 inset !important;
  box-shadow: 0 0 0px 1000px #0f1612 inset !important;
  transition: background-color 5000s ease-in-out 0s;
}

.commission-suffix {
  position: absolute;
  right: 14px;
  font-size: 0.9rem;
  font-weight: 800;
  color: var(--brand-green-light, #4ade80);
  pointer-events: none;
}

.eye-toggle-btn {
  position: absolute;
  right: 4px;
  top: 50%;
  transform: translateY(-50%);
  width: 38px;
  height: 38px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  cursor: pointer;
  border-radius: 8px;
  transition: background 0.15s ease;
  z-index: 3;
}

.eye-toggle-btn:hover {
  background: rgba(255, 255, 255, 0.08);
}

.input-hint {
  font-size: 0.74rem;
  color: #94a3b8;
  line-height: 1.35;
}

.checkbox-row {
  display: flex;
  align-items: center;
  gap: 9px;
  cursor: pointer;
  user-select: none;
  margin-top: 4px;
}

.custom-checkbox {
  accent-color: #22c55e;
  width: 17px;
  height: 17px;
  cursor: pointer;
}

.role-hint-box {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  background: rgba(34, 197, 94, 0.1);
  border: 1px solid rgba(34, 197, 94, 0.3);
  border-radius: 12px;
  padding: 12px 16px;
}

.code-pill {
  background: rgba(0, 0, 0, 0.5);
  border: 1px solid rgba(255, 255, 255, 0.15);
  padding: 1px 6px;
  border-radius: 5px;
  font-family: monospace;
  font-weight: 700;
  color: #4ade80;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.btn {
  padding: 10px 20px;
  border-radius: 10px;
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.18s ease;
  font-family: var(--font-heading);
}

.btn-outline {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.16);
  color: #cbd5e1;
}

.btn-outline:hover {
  background: rgba(255, 255, 255, 0.08);
  color: #ffffff;
}

.btn-primary {
  background: linear-gradient(135deg, #16a34a 0%, #15803d 100%);
  border: 1px solid rgba(34, 197, 94, 0.5);
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(22, 163, 74, 0.35);
}

.btn-primary:hover:not(:disabled) {
  background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
  transform: translateY(-1px);
  box-shadow: 0 6px 18px rgba(34, 197, 94, 0.45);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  display: inline-block;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.2s ease;
}

.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}

@media (max-width: 768px) {
  .modal-overlay {
    padding: 20px 16px;
  }
  .modal-card {
    max-width: calc(100vw - 32px);
    max-height: calc(100vh - 40px);
    padding: 20px;
    border-radius: 18px;
  }
}

@media (max-width: 480px) {
  .modal-overlay {
    padding: 16px 12px;
  }
  .modal-card {
    max-width: calc(100vw - 24px);
    max-height: calc(100vh - 32px);
    padding: 18px 16px;
    border-radius: 16px;
  }
}
</style>
