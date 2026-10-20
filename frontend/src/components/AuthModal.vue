<script setup lang="ts">
import { ref, onMounted } from 'vue';
import authService from '../services/auth';
import type { User, AuthSession } from '../types';

defineProps<{
  show: boolean;
  canClose?: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'login-success', session: User | AuthSession): void;
}>();

// Recordar usuario
const rememberedEmail = authService.getRememberedEmail();

// Estados del formulario de Login
const loginForm = ref({
  email: rememberedEmail || '',
  password: '',
  rememberUser: !!rememberedEmail
});

const showPassword = ref(false);
const loading = ref(false);
const errorMessage = ref('');

onMounted(() => {
  const saved = authService.getRememberedEmail();
  if (saved) {
    loginForm.value.email = saved;
    loginForm.value.rememberUser = true;
  }
});

const handleLogin = async () => {
  errorMessage.value = '';
  if (!loginForm.value.email || !loginForm.value.password) {
    errorMessage.value = 'Por favor ingresa tu correo y contraseña.';
    return;
  }

  try {
    loading.value = true;
    const result = await authService.login(
      loginForm.value.email,
      loginForm.value.password,
      loginForm.value.rememberUser
    );
    emit('login-success', result.user || result);
  } catch (err) {
    errorMessage.value = err.message || 'Error al iniciar sesión.';
  } finally {
    loading.value = false;
  }
};

const selectDemoAccount = (email, password = 'Admin123!') => {
  loginForm.value.email = email;
  loginForm.value.password = password;
  errorMessage.value = '';
};
</script>

<template>
  <transition name="modal-fade">
    <div v-if="show" class="auth-overlay" @click.self="canClose && $emit('close')">
      <div class="auth-card" role="dialog" aria-modal="true">
        <!-- Botón cerrar (si está habilitado) -->
        <button 
          v-if="canClose" 
          class="auth-close-btn" 
          @click="$emit('close')"
          title="Cerrar ventana"
        >
          &times;
        </button>

        <!-- Cabecera de Marca -->
        <div class="auth-header">
          <div class="brand-logo-wrapper">
            <img src="/logo.png" alt="Transporte Tito" class="auth-logo" />
          </div>
          <span class="auth-portal-badge">Portal de Acceso</span>
        </div>

        <!-- Mensaje de Error -->
        <transition name="fade">
          <div v-if="errorMessage" class="auth-error-banner">
            <span class="error-icon">⚠️</span>
            <span class="error-text">{{ errorMessage }}</span>
          </div>
        </transition>

        <!-- FORMULARIO DE INICIO DE SESIÓN -->
        <form @submit.prevent="handleLogin" class="auth-form">
          <!-- Cuentas de Acceso Rápido para Pruebas -->
          <div class="demo-accounts-card">
            <span class="demo-title">Acceso rápido por perfil:</span>
            <div class="demo-buttons">
              <button 
                type="button" 
                class="demo-chip"
                @click="selectDemoAccount('admin@titotransporte.com')"
                title="SuperAdmin / Propietario"
              >
                👑 SuperAdmin
              </button>
            </div>
          </div>
          <div class="form-group">
            <label class="form-label" for="login-email">Correo Electrónico / Usuario</label>
            <div class="input-wrapper">
              <span class="input-icon">✉️</span>
              <input 
                id="login-email"
                v-model="loginForm.email"
                type="email" 
                class="form-input" 
                placeholder="ej. admin@titotransporte.com"
                required
                autocomplete="email"
              />
            </div>
          </div>

          <div class="form-group">
            <div class="label-row">
              <label class="form-label" for="login-password">Contraseña</label>
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
                id="login-password"
                v-model="loginForm.password"
                :type="showPassword ? 'text' : 'password'" 
                class="form-input" 
                placeholder="Ingresa tu contraseña"
                required
                autocomplete="current-password"
              />
            </div>
          </div>

          <div class="form-row-remember">
            <label class="checkbox-label">
              <input type="checkbox" v-model="loginForm.rememberUser" class="custom-checkbox" />
              <span>Recordar mi usuario</span>
            </label>
          </div>

          <button type="submit" class="btn btn-primary btn-block submit-btn" :disabled="loading">
            <span v-if="loading" class="spinner"></span>
            <span v-else>Iniciar Sesión →</span>
          </button>
        </form>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.auth-overlay {
  position: fixed;
  inset: 0;
  z-index: 1500;
  background: radial-gradient(circle at 50% 25%, rgba(21, 128, 61, 0.16) 0%, rgba(12, 17, 14, 1) 75%), #0c110e;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px 20px;
  overflow-y: auto;
}

.auth-card {
  position: relative;
  width: 100%;
  max-width: 440px;
  max-height: calc(100vh - 48px);
  overflow-y: auto;
  margin: auto;
  background: linear-gradient(180deg, rgba(22, 32, 26, 0.98), rgba(14, 21, 17, 0.98));
  border: 1px solid var(--brand-border-strong);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.85), 0 0 40px rgba(34, 197, 94, 0.15);
  border-radius: var(--radius-lg, 18px);
  padding: 32px 28px;
  color: var(--text-main);
  animation: modalPop 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalPop {
  from {
    opacity: 0;
    transform: scale(0.94) translateY(12px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.auth-close-btn {
  position: absolute;
  top: 18px;
  right: 20px;
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 1.8rem;
  line-height: 1;
  cursor: pointer;
  transition: color var(--transition-fast);
  padding: 4px;
}

.demo-accounts-card {
  margin-bottom: 20px;
  padding: 12px 14px;
  background: rgba(34, 197, 94, 0.08);
  border: 1px solid rgba(34, 197, 94, 0.25);
  border-radius: var(--radius-md);
}

.demo-title {
  display: block;
  font-size: 0.78rem;
  color: var(--text-muted);
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-weight: 600;
}

.demo-buttons {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.demo-chip {
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid var(--brand-border);
  color: var(--text-main);
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 0.82rem;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.demo-chip:hover {
  background: var(--brand-green);
  color: #0b1510;
  border-color: var(--brand-green);
  font-weight: 600;
  transform: translateY(-1px);
}

.auth-close-btn:hover {
  color: #ffffff;
}

.auth-header {
  text-align: center;
  margin-bottom: 22px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.brand-logo-wrapper {
  margin-bottom: 10px;
  display: flex;
  justify-content: center;
  align-items: center;
}

.auth-logo {
  height: 110px;
  width: auto;
  max-width: 290px;
  object-fit: contain;
  filter: drop-shadow(0 6px 18px rgba(0, 0, 0, 0.5)) drop-shadow(0 0 25px rgba(34, 197, 94, 0.12));
  transition: transform var(--transition-normal);
}

.auth-logo:hover {
  transform: scale(1.02);
}

.auth-portal-badge {
  display: inline-block;
  padding: 3px 12px;
  border-radius: var(--radius-full);
  background: rgba(34, 197, 94, 0.1);
  border: 1px solid var(--brand-border);
  color: var(--brand-green-light);
  font-size: 0.74rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

/* Error Banner */
.auth-error-banner {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid rgba(239, 68, 68, 0.4);
  color: #fca5a5;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  margin-bottom: 18px;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 0.88rem;
}

.error-icon {
  font-size: 1.1rem;
}

/* Formularios */
.auth-form {
  display: flex;
  flex-direction: column;
  gap: 18px;
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

.toggle-pass-btn:hover {
  text-decoration: underline;
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

.form-input {
  width: 100%;
  padding: 11px 14px 11px 38px;
  background: rgba(12, 17, 14, 0.9);
  border: 1px solid var(--brand-border);
  border-radius: var(--radius-md);
  color: var(--text-main);
  font-size: 0.92rem;
  font-family: inherit;
  transition: all var(--transition-fast);
}

.form-input:focus {
  outline: none;
  border-color: var(--brand-green-light);
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.2);
  background: rgba(16, 24, 19, 0.95);
}

.form-row-remember {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.checkbox-label {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 0.86rem;
  color: var(--text-muted);
  user-select: none;
}

.custom-checkbox {
  accent-color: var(--brand-green-light);
  width: 16px;
  height: 16px;
  cursor: pointer;
}

/* Botón de envío */
.submit-btn {
  padding: 13px;
  font-size: 1rem;
  font-weight: 700;
  box-shadow: 0 6px 20px rgba(34, 197, 94, 0.35);
  margin-top: 4px;
}

.submit-btn:hover {
  box-shadow: 0 8px 25px rgba(34, 197, 94, 0.55);
}

.spinner {
  width: 20px;
  height: 20px;
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
  transition: opacity 0.25s ease;
}

.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}

@media (max-width: 768px) {
  .auth-overlay {
    padding: 20px 16px;
  }
  .auth-card {
    max-width: calc(100vw - 32px);
    max-height: calc(100vh - 40px);
    padding: 24px 20px;
    border-radius: 18px;
  }
}

@media (max-width: 480px) {
  .auth-overlay {
    padding: 16px 12px;
  }
  .auth-card {
    max-width: calc(100vw - 24px);
    max-height: calc(100vh - 32px);
    padding: 20px 16px;
    border-radius: 16px;
  }
}
</style>
