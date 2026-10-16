<script setup>
import { ref, onMounted } from 'vue';
import authService from '../services/auth';

defineProps({
  show: Boolean,
  canClose: {
    type: Boolean,
    default: true
  }
});

const emit = defineEmits(['close', 'login-success']);

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
          <div class="brand-badge">
            <img src="/logo.png" alt="Transporte Tito" class="auth-logo" />
          </div>
          <h2 class="auth-title">Transporte Tito</h2>
          <p class="auth-slogan">"Servirle es nuestra prioridad"</p>
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
  background: rgba(8, 12, 10, 0.88);
  backdrop-filter: blur(14px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  overflow-y: auto;
}

.auth-card {
  position: relative;
  width: 100%;
  max-width: 440px;
  background: linear-gradient(180deg, rgba(22, 32, 26, 0.98), rgba(14, 21, 17, 0.98));
  border: 1px solid var(--brand-border-strong);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.85), 0 0 40px rgba(34, 197, 94, 0.15);
  border-radius: var(--radius-lg);
  padding: 34px 28px;
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

.auth-close-btn:hover {
  color: #ffffff;
}

.auth-header {
  text-align: center;
  margin-bottom: 24px;
}

.brand-badge {
  display: inline-flex;
  padding: 8px;
  border-radius: 50%;
  background: rgba(34, 197, 94, 0.12);
  border: 1px solid rgba(34, 197, 94, 0.3);
  margin-bottom: 10px;
  box-shadow: 0 0 20px rgba(34, 197, 94, 0.2);
}

.auth-logo {
  width: 52px;
  height: 52px;
  object-fit: contain;
  border-radius: 50%;
}

.auth-title {
  font-size: 1.55rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  background: linear-gradient(135deg, #ffffff, #86efac);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  margin-bottom: 4px;
}

.auth-slogan {
  font-size: 0.86rem;
  color: var(--brand-green-light);
  font-style: italic;
  font-weight: 500;
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
</style>
