<script setup lang="ts">
defineProps<{
  show: boolean;
  message?: string;
  type?: 'success' | 'error' | 'info' | 'warning' | string;
}>();

defineEmits<{
  (e: 'close'): void;
}>();
</script>

<template>
  <transition name="toast-slide">
    <div v-if="show" :class="['toast-banner', type]">
      <div class="toast-icon">
        <span v-if="type === 'success'">✓</span>
        <span v-else-if="type === 'error'">✕</span>
        <span v-else>ℹ</span>
      </div>
      <div class="toast-message">{{ message }}</div>
      <button class="toast-close" @click="$emit('close')">&times;</button>
    </div>
  </transition>
</template>

<style scoped>
.toast-banner {
  position: fixed;
  bottom: 80px;
  right: 20px;
  z-index: 2000;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 18px;
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-elevated);
  backdrop-filter: blur(12px);
  max-width: 90vw;
  width: 380px;
}

@media (min-width: 769px) {
  .toast-banner {
    bottom: auto;
    top: 24px;
    right: 24px;
  }
}

.toast-banner.success {
  background: rgba(19, 35, 25, 0.95);
  border: 1px solid var(--brand-green-light);
  color: #ffffff;
}

.toast-banner.error {
  background: rgba(45, 18, 18, 0.95);
  border: 1px solid #ef4444;
  color: #fee2e2;
}

.toast-icon {
  width: 26px;
  height: 26px;
  border-radius: var(--radius-full);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 0.85rem;
}

.success .toast-icon {
  background: var(--brand-green);
  color: #ffffff;
}

.error .toast-icon {
  background: #ef4444;
  color: #ffffff;
}

.toast-message {
  flex: 1;
  font-size: 0.9rem;
  font-weight: 500;
}

.toast-close {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 1.3rem;
  cursor: pointer;
  line-height: 1;
  padding: 2px 6px;
}

.toast-close:hover {
  color: #ffffff;
}

.toast-slide-enter-active,
.toast-slide-leave-active {
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-slide-enter-from {
  opacity: 0;
  transform: translateY(20px) scale(0.95);
}

.toast-slide-leave-to {
  opacity: 0;
  transform: translateY(-20px) scale(0.95);
}
</style>
