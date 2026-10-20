<script setup lang="ts">
import { ref, computed } from 'vue';
import { authService } from '../services/auth';
import type { User } from '../types';

const props = defineProps<{
  show: boolean;
  currentUser?: User | null;
}>();

const emit = defineEmits<{
  (e: 'password-changed'): void;
  (e: 'logout'): void;
}>();

const currentPassword = ref('');
const newPassword = ref('');
const confirmPassword = ref('');
const showNewPass = ref(false);
const loading = ref(false);
const errorMessage = ref('');

// Inicializar contraseña temporal con el código si está disponible
const initialCode = computed(() => props.currentUser?.code || '');

// Indicador de seguridad de la nueva contraseña
const passwordStrength = computed(() => {
  const p = newPassword.value;
  if (!p) return { score: 0, label: 'Sin ingresar', color: 'bg-slate-700' };
  if (p.length < 6) return { score: 1, label: 'Muy corta (mínimo 6)', color: 'bg-red-500' };
  
  const hasLetter = /[a-zA-Z]/.test(p);
  const hasNumber = /[0-9]/.test(p);
  const hasSpecial = /[^A-Za-z0-9]/.test(p);

  if (p.length >= 8 && hasLetter && hasNumber && hasSpecial) {
    return { score: 3, label: 'Excelente y Segura', color: 'bg-emerald-400' };
  }
  if (p.length >= 6 && hasLetter && hasNumber) {
    return { score: 2, label: 'Buena', color: 'bg-amber-400' };
  }
  return { score: 1, label: 'Básica', color: 'bg-yellow-500' };
});

const handleSubmit = async () => {
  errorMessage.value = '';

  const current = currentPassword.value.trim() || initialCode.value;
  if (!current) {
    errorMessage.value = 'Debes ingresar tu contraseña actual o código temporal.';
    return;
  }

  if (!newPassword.value || newPassword.value.length < 6) {
    errorMessage.value = 'La nueva contraseña debe tener al menos 6 caracteres.';
    return;
  }

  if (newPassword.value === current) {
    errorMessage.value = 'La nueva contraseña no puede ser igual a tu código temporal.';
    return;
  }

  if (newPassword.value !== confirmPassword.value) {
    errorMessage.value = 'La confirmación de la contraseña no coincide.';
    return;
  }

  try {
    loading.value = true;
    await authService.changePassword(current, newPassword.value);
    emit('password-changed');
  } catch (err: any) {
    errorMessage.value = err.message || 'Error al actualizar la contraseña.';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <transition name="modal-fade">
    <div v-if="show" class="first-login-overlay" role="dialog" aria-modal="true" aria-labelledby="first-login-title">
      <div class="first-login-card">
        <!-- Encabezado con Icono de Seguridad -->
        <div class="card-header">
          <div class="shield-badge">
            <svg class="w-7 h-7 text-brand-green-light" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
              <path d="m9 12 2 2 4-4"/>
            </svg>
          </div>
          <div>
            <span class="step-tag">Primer Acceso a la Plataforma</span>
            <h2 id="first-login-title" class="title">Actualiza tu Contraseña</h2>
            <p class="subtitle">
              ¡Bienvenido, <strong>{{ currentUser?.fullName }}</strong>! Tu cuenta fue generada con una clave temporal (tu código <code>{{ initialCode || 'CHO-00X' }}</code>). Por tu seguridad, define tu contraseña personal para continuar.
            </p>
          </div>
        </div>

        <!-- Banner de Error -->
        <div v-if="errorMessage" class="error-banner">
          <svg class="w-5 h-5 shrink-0 text-red-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
          </svg>
          <span>{{ errorMessage }}</span>
        </div>

        <form @submit.prevent="handleSubmit" class="form-body" autocomplete="off">
          <!-- 1. Contraseña actual / temporal -->
          <div class="form-group">
            <label class="form-label" for="current-temp-pass">
              Contraseña Temporal (Código: {{ initialCode || 'Asignado' }}) *
            </label>
            <div class="input-wrapper">
              <span class="input-icon">
                <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect width="18" height="11" x="3" y="11" rx="2" ry="2"/>
                  <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </span>
              <input 
                id="current-temp-pass" 
                v-model="currentPassword" 
                type="text" 
                class="form-input with-icon font-mono" 
                :placeholder="initialCode ? `Ingresa ${initialCode}` : 'Código temporal de tu cuenta'" 
                autocomplete="current-password"
                required 
              />
            </div>
          </div>

          <!-- 2. Nueva Contraseña -->
          <div class="form-group">
            <div class="label-row">
              <label class="form-label" for="new-personal-pass">Nueva Contraseña Personal *</label>
              <span class="strength-text" :class="passwordStrength.score >= 2 ? 'text-emerald-400' : 'text-slate-400'">
                {{ passwordStrength.label }}
              </span>
            </div>
            <div class="input-wrapper">
              <span class="input-icon">
                <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm0 18a8 8 0 1 1 8-8 8 8 0 0 1-8 8z"/>
                  <path d="m9 12 2 2 4-4"/>
                </svg>
              </span>
              <input 
                id="new-personal-pass" 
                v-model="newPassword" 
                :type="showNewPass ? 'text' : 'password'" 
                class="form-input with-icon pr-12" 
                placeholder="Mínimo 6 caracteres (letras y números)" 
                autocomplete="new-password"
                required 
              />
              <button 
                type="button" 
                class="eye-btn" 
                @click="showNewPass = !showNewPass"
                :title="showNewPass ? 'Ocultar' : 'Ver contraseña'"
                aria-label="Alternar visibilidad de contraseña"
              >
                <svg v-if="showNewPass" class="w-4 h-4 text-slate-300" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M9.88 9.88a3 3 0 1 0 4.24 4.24"/>
                  <path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68"/>
                  <path d="M6.61 6.61A13.526 13.526 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61"/>
                  <line x1="2" y1="22" x2="22" y2="2"/>
                </svg>
                <svg v-else class="w-4 h-4 text-slate-300" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
              </button>
            </div>

            <!-- Barra indicadora de fortaleza -->
            <div class="strength-bar-container">
              <div 
                class="strength-bar-fill transition-all duration-300" 
                :class="passwordStrength.color"
                :style="{ width: passwordStrength.score === 0 ? '0%' : (passwordStrength.score === 1 ? '33%' : (passwordStrength.score === 2 ? '66%' : '100%')) }"
              ></div>
            </div>
          </div>

          <!-- 3. Confirmar Nueva Contraseña -->
          <div class="form-group">
            <label class="form-label" for="confirm-personal-pass">Confirmar Nueva Contraseña *</label>
            <div class="input-wrapper">
              <span class="input-icon">
                <svg class="w-4 h-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect width="18" height="11" x="3" y="11" rx="2" ry="2"/>
                  <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </span>
              <input 
                id="confirm-personal-pass" 
                v-model="confirmPassword" 
                :type="showNewPass ? 'text' : 'password'" 
                class="form-input with-icon" 
                placeholder="Repite tu nueva contraseña" 
                autocomplete="new-password"
                required 
              />
            </div>
          </div>

          <!-- Acciones -->
          <div class="actions-row">
            <button type="button" class="btn btn-logout" @click="$emit('logout')">
              Cerrar Sesión
            </button>
            <button type="submit" class="btn btn-submit" :disabled="loading">
              <span v-if="loading" class="spinner"></span>
              <span v-else>Guardar y Entrar</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.first-login-overlay {
  position: fixed;
  inset: 0;
  z-index: 2000;
  background: rgba(4, 7, 5, 0.94);
  backdrop-filter: blur(16px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px 20px;
  overflow-y: auto;
}

.first-login-card {
  width: 100%;
  max-width: 520px;
  max-height: calc(100vh - 48px);
  overflow-y: auto;
  margin: auto;
  background: linear-gradient(180deg, #141f18 0%, #0c120e 100%);
  border: 1px solid rgba(34, 197, 94, 0.35);
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.95), 0 0 35px rgba(34, 197, 94, 0.18);
  border-radius: 18px;
  padding: 26px 28px;
  color: #ffffff;
  animation: cardIn 0.24s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes cardIn {
  from { opacity: 0; transform: scale(0.96) translateY(12px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}

.card-header {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 20px;
}

.shield-badge {
  width: 50px;
  height: 50px;
  border-radius: 14px;
  background: rgba(34, 197, 94, 0.15);
  border: 1px solid rgba(34, 197, 94, 0.35);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.step-tag {
  display: inline-block;
  font-size: 0.72rem;
  font-weight: 800;
  color: #4ade80;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  margin-bottom: 3px;
}

.title {
  font-size: 1.3rem;
  font-weight: 800;
  color: #ffffff;
  font-family: var(--font-heading);
  line-height: 1.2;
}

.subtitle {
  font-size: 0.82rem;
  color: #94a3b8;
  line-height: 1.4;
  margin-top: 4px;
}

.subtitle code {
  background: rgba(0, 0, 0, 0.5);
  border: 1px solid rgba(255, 255, 255, 0.14);
  padding: 1px 5px;
  border-radius: 4px;
  color: #4ade80;
  font-family: monospace;
}

.error-banner {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid rgba(239, 68, 68, 0.4);
  color: #fca5a5;
  padding: 10px 14px;
  border-radius: 10px;
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 0.84rem;
}

.form-body {
  display: flex;
  flex-direction: column;
  gap: 15px;
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
  font-size: 0.76rem;
  font-weight: 700;
  color: #cbd5e1;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.strength-text {
  font-size: 0.74rem;
  font-weight: 600;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.input-icon {
  position: absolute;
  left: 13px;
  pointer-events: none;
  display: flex;
  align-items: center;
  z-index: 2;
}

.form-input {
  width: 100%;
  padding: 11px 14px;
  background: #0f1612;
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 10px;
  color: #ffffff;
  font-size: 0.92rem;
  font-family: inherit;
  transition: all 0.18s ease;
}

.form-input.with-icon {
  padding-left: 38px;
}

.form-input::placeholder {
  color: #64748b;
  opacity: 1;
}

.form-input:focus {
  outline: none;
  border-color: #22c55e;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.22);
}

.eye-btn {
  position: absolute;
  right: 5px;
  top: 50%;
  transform: translateY(-50%);
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  cursor: pointer;
  border-radius: 8px;
  transition: background 0.15s ease;
}

.eye-btn:hover {
  background: rgba(255, 255, 255, 0.08);
}

.strength-bar-container {
  width: 100%;
  height: 4px;
  background: rgba(255, 255, 255, 0.08);
  border-radius: 4px;
  overflow: hidden;
  margin-top: 4px;
}

.strength-bar-fill {
  height: 100%;
  border-radius: 4px;
}

.actions-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 10px;
  padding-top: 12px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.btn {
  padding: 11px 20px;
  border-radius: 10px;
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.18s ease;
  font-family: var(--font-heading);
}

.btn-logout {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.16);
  color: #94a3b8;
}

.btn-logout:hover {
  background: rgba(239, 68, 68, 0.12);
  border-color: rgba(239, 68, 68, 0.35);
  color: #fca5a5;
}

.btn-submit {
  flex: 1;
  background: linear-gradient(135deg, #16a34a 0%, #15803d 100%);
  border: 1px solid rgba(34, 197, 94, 0.5);
  color: #ffffff;
  box-shadow: 0 4px 14px rgba(22, 163, 74, 0.35);
}

.btn-submit:hover:not(:disabled) {
  background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
  transform: translateY(-1px);
  box-shadow: 0 6px 18px rgba(34, 197, 94, 0.45);
}

.btn-submit:disabled {
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
  transition: opacity 0.22s ease;
}

.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}

@media (max-width: 768px) {
  .first-login-overlay {
    padding: 20px 16px;
  }
  .first-login-card {
    max-width: calc(100vw - 32px);
    max-height: calc(100vh - 40px);
    padding: 20px;
    border-radius: 18px;
  }
}

@media (max-width: 480px) {
  .first-login-overlay {
    padding: 16px 12px;
  }
  .first-login-card {
    max-width: calc(100vw - 24px);
    max-height: calc(100vh - 32px);
    padding: 18px 16px;
    border-radius: 16px;
  }
}
</style>
