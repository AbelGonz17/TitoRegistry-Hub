<script setup>
import { ref, computed, watch } from 'vue';

const props = defineProps({
  show: Boolean,
  employee: {
    type: Object,
    default: () => ({ id: 1, fullName: 'Chofer', commissionRate: 25 })
  },
  serviceTypes: {
    type: Array,
    default: () => []
  },
  loading: Boolean
});

const emit = defineEmits(['close', 'submit']);

// Formulario reactivo
const formData = ref({
  serviceTypeId: '',
  clientName: '',
  clientAddress: '',
  clientPhone: '',
  serviceDate: new Date().toISOString().slice(0, 10),
  priceCharged: '',
  notes: ''
});

const errorMessage = ref('');

// Al abrir o cambiar tipos de servicio, inicializar selección
watch(() => props.show, (newVal) => {
  if (newVal) {
    errorMessage.value = '';
    formData.value = {
      serviceTypeId: props.serviceTypes.length > 0 ? props.serviceTypes[0].id : '',
      clientName: '',
      clientAddress: '',
      clientPhone: '',
      serviceDate: new Date().toISOString().slice(0, 10),
      priceCharged: props.serviceTypes.length > 0 ? props.serviceTypes[0].defaultPrice : '',
      notes: ''
    };
  }
});

// Cuando cambia el tipo de servicio seleccionado, sugerir el precio base del catálogo
const onServiceTypeChange = (event) => {
  const typeId = Number(event.target.value);
  formData.value.serviceTypeId = typeId;
  const selected = props.serviceTypes.find(s => s.id === typeId);
  if (selected && (!formData.value.priceCharged || Number(formData.value.priceCharged) === 0)) {
    formData.value.priceCharged = selected.defaultPrice;
  }
};

// Cálculo en vivo de lo que acumulará el chofer
const estimatedEarnings = computed(() => {
  const price = parseFloat(formData.value.priceCharged) || 0;
  const rate = props.employee?.commissionRate || 0;
  return ((price * rate) / 100).toFixed(2);
});

const handleSubmit = () => {
  errorMessage.value = '';

  if (!formData.value.serviceTypeId) {
    errorMessage.value = 'Por favor selecciona un tipo de servicio o ruta.';
    return;
  }
  if (!formData.value.clientName.trim()) {
    errorMessage.value = 'El nombre del cliente es obligatorio.';
    return;
  }
  const price = parseFloat(formData.value.priceCharged);
  if (isNaN(price) || price <= 0) {
    errorMessage.value = 'El monto cobrado debe ser un valor válido mayor a 0.';
    return;
  }

  const payload = {
    employeeId: props.employee.id,
    serviceTypeId: Number(formData.value.serviceTypeId),
    clientName: formData.value.clientName.trim(),
    clientAddress: formData.value.clientAddress.trim(),
    clientPhone: formData.value.clientPhone.trim(),
    serviceDate: new Date(formData.value.serviceDate).toISOString(),
    priceCharged: price,
    notes: formData.value.notes.trim()
  };

  emit('submit', payload);
};
</script>

<template>
  <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <!-- Encabezado del Modal -->
      <div class="modal-header">
        <div class="header-titles">
          <div class="badge-tag">🚐 Transporte Tito</div>
          <h2 class="modal-title">Registrar Nuevo Viaje / Servicio</h2>
          <p class="modal-subtitle">
            Chofer: <strong>{{ employee.fullName }}</strong> (Tasa: {{ employee.commissionRate }}%)
          </p>
        </div>
        <button class="btn-close" @click="$emit('close')" aria-label="Cerrar">&times;</button>
      </div>

      <!-- Formulario -->
      <form @submit.prevent="handleSubmit" class="modal-body">
        <div v-if="errorMessage" class="alert-error">
          <span>⚠️</span>
          <span>{{ errorMessage }}</span>
        </div>

        <!-- Tipo de Servicio / Ruta -->
        <div class="form-group">
          <label class="form-label">Tipo de Servicio / Ruta *</label>
          <select 
            class="form-control" 
            :value="formData.serviceTypeId" 
            @change="onServiceTypeChange"
            required
          >
            <option value="" disabled>Selecciona la ruta o servicio</option>
            <option v-for="st in serviceTypes" :key="st.id" :value="st.id">
              {{ st.name }} ({{ st.category }}) — Base: ${{ st.defaultPrice }}
            </option>
          </select>
          <p v-if="serviceTypes.length === 0" class="field-hint">
            * No hay rutas activas. Puedes crearlas en la pestaña "Catálogo de Rutas".
          </p>
        </div>

        <!-- Cliente -->
        <div class="form-group">
          <label class="form-label">Nombre del Cliente / Empresa *</label>
          <input 
            v-model="formData.clientName"
            type="text" 
            class="form-control" 
            placeholder="Ej: Lic. Carlos Pérez / Grupo Ramos" 
            required
          />
        </div>

        <!-- Origen y Destino -->
        <div class="form-row">
          <div class="form-group flex-2">
            <label class="form-label">Ruta / Origen - Destino</label>
            <input 
              v-model="formData.clientAddress"
              type="text" 
              class="form-control" 
              placeholder="Ej: Santo Domingo -> Santiago (Los Jardines)" 
            />
          </div>
          <div class="form-group flex-1">
            <label class="form-label">Teléfono Cliente</label>
            <input 
              v-model="formData.clientPhone"
              type="tel" 
              class="form-control" 
              placeholder="+1 (809)..." 
            />
          </div>
        </div>

        <!-- Fecha y Precio Cobrado -->
        <div class="form-row">
          <div class="form-group flex-1">
            <label class="form-label">Fecha del Viaje *</label>
            <input 
              v-model="formData.serviceDate"
              type="date" 
              class="form-control" 
              required
            />
          </div>
          <div class="form-group flex-1">
            <label class="form-label">Monto Cobrado ($) *</label>
            <input 
              v-model="formData.priceCharged"
              type="number" 
              step="0.01" 
              min="0.01" 
              class="form-control price-input" 
              placeholder="0.00" 
              required
            />
          </div>
        </div>

        <!-- Caja de Cálculo en Vivo de Acumulados -->
        <div class="earnings-preview-card">
          <div class="preview-header">
            <span class="preview-badge">Cálculo de Comisión en Tiempo Real</span>
            <span class="rate-tag">{{ employee.commissionRate }}% Asignado</span>
          </div>
          <div class="preview-body">
            <div class="preview-amount">
              <span class="currency">$</span>
              <span class="val">{{ estimatedEarnings }}</span>
            </div>
            <p class="preview-desc">
              Esta es la ganancia que sumarás directamente a tu <strong>Total Acumulado</strong> al completar este viaje.
            </p>
          </div>
        </div>

        <!-- Notas / Observaciones -->
        <div class="form-group">
          <label class="form-label">Observaciones del Viaje</label>
          <textarea 
            v-model="formData.notes"
            class="form-control" 
            rows="2" 
            placeholder="Detalles de paradas, peajes, condiciones de entrega o retorno..."
          ></textarea>
        </div>

        <!-- Botones de Acción -->
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="$emit('close')">
            Cancelar
          </button>
          <button type="submit" class="btn btn-primary" :disabled="loading">
            <span v-if="loading">Guardando viaje...</span>
            <span v-else>Confirmar y Registrar Viaje</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.modal-header {
  padding: 20px 24px;
  border-bottom: 1px solid var(--brand-border);
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}

.badge-tag {
  font-size: 0.72rem;
  color: var(--brand-green-light);
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 4px;
}

.modal-title {
  font-size: 1.25rem;
  color: #ffffff;
  margin-bottom: 4px;
}

.modal-subtitle {
  font-size: 0.85rem;
  color: var(--text-muted);
}

.btn-close {
  background: transparent;
  border: none;
  font-size: 1.7rem;
  color: var(--text-muted);
  cursor: pointer;
  line-height: 1;
  padding: 4px;
}

.btn-close:hover {
  color: #ffffff;
}

.modal-body {
  padding: 20px 24px;
}

.alert-error {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid #ef4444;
  color: #fca5a5;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  margin-bottom: 16px;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-row {
  display: flex;
  gap: 14px;
}

.flex-1 { flex: 1; }
.flex-2 { flex: 2; }

.price-input {
  font-family: var(--font-heading);
  font-weight: 700;
  color: var(--brand-green-light);
  font-size: 1.1rem;
}

.field-hint {
  font-size: 0.75rem;
  color: #fbbf24;
  margin-top: 4px;
}

/* Caja de previsualización de comisión */
.earnings-preview-card {
  background: linear-gradient(135deg, rgba(21, 128, 61, 0.2), rgba(34, 197, 94, 0.05));
  border: 1px solid var(--brand-border-strong);
  border-radius: var(--radius-md);
  padding: 14px 18px;
  margin: 18px 0;
}

.preview-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.preview-badge {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--brand-green-light);
  text-transform: uppercase;
}

.rate-tag {
  background: var(--brand-green);
  color: #ffffff;
  padding: 2px 8px;
  border-radius: var(--radius-full);
  font-size: 0.75rem;
  font-weight: 700;
}

.preview-body {
  display: flex;
  align-items: center;
  gap: 16px;
}

.preview-amount {
  display: flex;
  align-items: baseline;
  font-family: var(--font-heading);
  font-weight: 800;
  color: #ffffff;
  text-shadow: 0 0 15px var(--brand-green-glow);
}

.preview-amount .currency {
  font-size: 1.1rem;
  color: var(--brand-green-light);
  margin-right: 2px;
}

.preview-amount .val {
  font-size: 1.7rem;
}

.preview-desc {
  font-size: 0.8rem;
  color: var(--text-muted);
  line-height: 1.3;
}

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid var(--brand-border);
}

@media (max-width: 600px) {
  .form-row {
    flex-direction: column;
    gap: 0;
  }
  .modal-body {
    padding: 16px;
  }
  .preview-body {
    flex-direction: column;
    align-items: flex-start;
    gap: 6px;
  }
}
</style>
