<script setup lang="ts">
import { ref, watch, computed } from 'vue';
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
  password: '',
  phone: '',
  role: 'Chofer',
  department: 'Rutas Interurbanas',
  commissionRate: 15,
  isActive: true,
  avatar: ''
});

const showPassword = ref(false);
const errorMessage = ref('');

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
        department: newVal.department || '',
        commissionRate: newVal.commissionRate ?? (newVal.role === 'Chofer' ? 15 : 0),
        isActive: newVal.isActive !== false,
        avatar: newVal.avatar || ''
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
        commissionRate: 15,
        isActive: true,
        avatar: ''
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
    if (form.value.commissionRate === 0) form.value.commissionRate = 15;
    if (!form.value.department || form.value.department.includes('Dirección')) {
      form.value.department = 'Rutas Interurbanas';
    }
  } else if (form.value.role === 'Administrador') {
    form.value.commissionRate = 0;
    if (!form.value.department || form.value.department.includes('Rutas')) {
      form.value.department = 'Operaciones y Logística';
    }
  }
};

const handleSubmit = () => {
  errorMessage.value = '';

  if (!form.value.fullName || form.value.fullName.trim().length < 3) {
    errorMessage.value = 'El nombre completo debe tener al menos 3 caracteres.';
    return;
  }

  if (!form.value.email || !form.value.email.includes('@')) {
    errorMessage.value = 'Por favor ingresa un correo electrónico válido.';
    return;
  }

  if (!isEditing.value && (!form.value.password || form.value.password.length < 6)) {
    errorMessage.value = 'La contraseña inicial debe tener al menos 6 caracteres.';
    return;
  }

  // Si está editando y puso password menor a 6
  if (isEditing.value && form.value.password && form.value.password.length < 6) {
    errorMessage.value = 'La nueva contraseña debe tener al menos 6 caracteres.';
    return;
  }

  const payload = {
    fullName: form.value.fullName.trim(),
    email: form.value.email.trim().toLowerCase(),
    phone: form.value.phone ? form.value.phone.trim() : '',
    role: form.value.role,
    department: form.value.department ? form.value.department.trim() : 'Operaciones',
    commissionRate: form.value.role === 'Chofer' ? Number(form.value.commissionRate) || 0 : 0,
    isActive: form.value.isActive,
    avatar: form.value.role === 'Chofer' ? '🚚' : (form.value.role === 'SuperAdmin' ? '👑' : '👔')
  };

  if (form.value.password) {
    payload.password = form.value.password;
  }

  emit('submit', payload);
};
</script>

<template>
  <transition name="modal-fade">
    <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
      <div class="modal-card" role="dialog" aria-modal="true">
        <!-- Encabezado del modal -->
        <div class="modal-header">
          <div class="header-icon">
            <span>{{ form.role === 'Chofer' ? '🚚' : (form.role === 'SuperAdmin' ? '👑' : '👔') }}</span>
          </div>
          <div>
            <h3 class="modal-title">{{ isEditing ? 'Editar Usuario' : 'Crear Nuevo Usuario' }}</h3>
            <p class="modal-subtitle">
              {{ isEditing ? `Modificando los datos de ${props.user?.fullName}` : 'Registra un Administrador o Chofer para la flota de Transporte Tito' }}
            </p>
          </div>
          <button class="modal-close" @click="$emit('close')" title="Cerrar modal">&times;</button>
        </div>

        <!-- Banner de Error -->
        <transition name="fade">
          <div v-if="errorMessage" class="error-banner">
            <span>⚠️</span>
            <span>{{ errorMessage }}</span>
          </div>
        </transition>

        <!-- Formulario -->
        <form @submit.prevent="handleSubmit" class="modal-form">
          <div class="form-grid-2">
            <!-- Nombre Completo -->
            <div class="form-group">
              <label class="form-label" for="user-name">Nombre y Apellido *</label>
              <input 
                id="user-name" 
                v-model="form.fullName" 
                type="text" 
                class="form-input" 
                placeholder="ej. Carlos Rosario" 
                required 
              />
            </div>

            <!-- Rol -->
            <div class="form-group">
              <label class="form-label" for="user-role">Rol en la Empresa *</label>
              <select id="user-role" v-model="form.role" class="form-select" @change="onRoleChange">
                <option value="Chofer">🚚 Chofer / Conductor</option>
                <option value="Administrador">👔 Administrador de Flota</option>
                <option value="SuperAdmin">👑 Super Administrador</option>
              </select>
            </div>
          </div>

          <div class="form-grid-2">
            <!-- Correo Electrónico -->
            <div class="form-group">
              <label class="form-label" for="user-email">Correo Electrónico *</label>
              <input 
                id="user-email" 
                v-model="form.email" 
                type="email" 
                class="form-input" 
                placeholder="carlos.rosario@titotransporte.com" 
                required 
                :disabled="isEditing"
              />
              <span v-if="isEditing" class="input-hint">El correo no puede modificarse una vez creado.</span>
            </div>

            <!-- Teléfono -->
            <div class="form-group">
              <label class="form-label" for="user-phone">Teléfono / Celular</label>
              <input 
                id="user-phone" 
                v-model="form.phone" 
                type="tel" 
                class="form-input" 
                placeholder="809-555-0303" 
              />
            </div>
          </div>

          <!-- Contraseña -->
          <div class="form-group">
            <div class="label-row">
              <label class="form-label" for="user-pass">
                {{ isEditing ? 'Nueva Contraseña (opcional)' : 'Contraseña Inicial *' }}
              </label>
              <button 
                type="button" 
                class="toggle-pass-btn" 
                @click="showPassword = !showPassword"
              >
                {{ showPassword ? 'Ocultar' : 'Mostrar' }}
              </button>
            </div>
            <div class="input-wrapper">
              <span class="input-icon">🔒</span>
              <input 
                id="user-pass" 
                v-model="form.password" 
                :type="showPassword ? 'text' : 'password'" 
                class="form-input" 
                :placeholder="isEditing ? 'Dejar en blanco para mantener la actual' : 'Mínimo 6 caracteres'" 
                :required="!isEditing"
              />
            </div>
          </div>

          <div class="form-grid-2">
            <!-- Departamento / Zona -->
            <div class="form-group">
              <label class="form-label" for="user-dept">Ruta / Departamento</label>
              <input 
                id="user-dept" 
                v-model="form.department" 
                type="text" 
                class="form-input" 
                placeholder="ej. Rutas Interurbanas / Cibao" 
              />
            </div>

            <!-- Tasa de Comisión (Solo visible/relevante para choferes) -->
            <div class="form-group" v-if="form.role === 'Chofer'">
              <label class="form-label" for="user-commission">Comisión de Viajes (%)</label>
              <div class="input-wrapper">
                <span class="input-icon">💰</span>
                <input 
                  id="user-commission" 
                  v-model.number="form.commissionRate" 
                  type="number" 
                  min="0" 
                  max="100" 
                  step="0.5" 
                  class="form-input" 
                  placeholder="15.0" 
                />
              </div>
            </div>

            <!-- Estado Activo (solo en edición) -->
            <div class="form-group" v-if="isEditing">
              <label class="form-label">Estado de la Cuenta</label>
              <label class="checkbox-row">
                <input type="checkbox" v-model="form.isActive" class="custom-checkbox" />
                <span>Usuario Activo en el Sistema</span>
              </label>
            </div>
          </div>

          <!-- Nota de Chofer -->
          <div v-if="form.role === 'Chofer'" class="role-hint-box">
            <span>ℹ️</span>
            <span>Al crear un <strong>Chofer</strong>, el backend le asignará automáticamente su código único (ej. <code>CHO-00X</code>) y sincronizará su cuenta con el registro de viajes.</span>
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
  background: rgba(8, 12, 10, 0.85);
  backdrop-filter: blur(12px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 18px;
  overflow-y: auto;
}

.modal-card {
  position: relative;
  width: 100%;
  max-width: 580px;
  background: linear-gradient(180deg, rgba(22, 32, 26, 0.98), rgba(14, 21, 17, 0.98));
  border: 1px solid var(--brand-border-strong);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.85), 0 0 35px rgba(34, 197, 94, 0.15);
  border-radius: var(--radius-lg);
  padding: 28px 24px;
  color: var(--text-main);
  animation: modalScale 0.22s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalScale {
  from { opacity: 0; transform: scale(0.95) translateY(10px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}

.modal-header {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 20px;
  position: relative;
}

.header-icon {
  width: 44px;
  height: 44px;
  border-radius: var(--radius-md);
  background: rgba(34, 197, 94, 0.15);
  border: 1px solid rgba(34, 197, 94, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  flex-shrink: 0;
}

.modal-title {
  font-size: 1.3rem;
  font-weight: 800;
  color: #ffffff;
  margin-bottom: 2px;
}

.modal-subtitle {
  font-size: 0.82rem;
  color: var(--text-muted);
}

.modal-close {
  position: absolute;
  top: 0;
  right: 0;
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 1.8rem;
  line-height: 1;
  cursor: pointer;
}

.modal-close:hover {
  color: #ffffff;
}

.error-banner {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid rgba(239, 68, 68, 0.4);
  color: #fca5a5;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 0.88rem;
}

.modal-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.form-grid-2 {
  display: grid;
  grid-template-columns: 1fr;
  gap: 14px;
}

@media (min-width: 520px) {
  .form-grid-2 {
    grid-template-columns: 1fr 1fr;
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.label-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.form-label {
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.toggle-pass-btn {
  background: transparent;
  border: none;
  color: var(--brand-green-light);
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  padding: 0;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.input-icon {
  position: absolute;
  left: 12px;
  font-size: 0.95rem;
  pointer-events: none;
  opacity: 0.75;
}

.form-input, .form-select {
  width: 100%;
  padding: 10px 14px;
  background: rgba(12, 17, 14, 0.9);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  color: var(--text-main);
  font-size: 0.92rem;
  font-family: inherit;
  transition: all var(--transition-fast);
}

.input-wrapper .form-input {
  padding-left: 36px;
}

.form-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  background: rgba(20, 28, 23, 0.5);
}

.form-input:focus, .form-select:focus {
  outline: none;
  border-color: var(--brand-green-light);
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.2);
}

.input-hint {
  font-size: 0.74rem;
  color: var(--text-dim);
}

.checkbox-row {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 0.86rem;
  color: var(--text-main);
  margin-top: 6px;
}

.custom-checkbox {
  accent-color: var(--brand-green-light);
  width: 16px;
  height: 16px;
  cursor: pointer;
}

.role-hint-box {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  background: rgba(34, 197, 94, 0.08);
  border: 1px solid rgba(34, 197, 94, 0.25);
  border-radius: var(--radius-md);
  padding: 10px 14px;
  font-size: 0.8rem;
  color: #a7f3d0;
  line-height: 1.4;
}

.role-hint-box code {
  background: rgba(0, 0, 0, 0.4);
  padding: 2px 6px;
  border-radius: 4px;
  color: #ffffff;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 8px;
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
</style>
