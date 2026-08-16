<script setup lang="ts">
import { ref } from 'vue';
import type { LoginCredentials } from './types';

interface Props {
  title?: string;
  usernameLabel?: string;
  passwordLabel?: string;
  okText?: string;
  cancelText?: string;
  showCancel?: boolean;
}

withDefaults(defineProps<Props>(), {
  title: 'User Authentication',
  usernameLabel: 'Username:',
  passwordLabel: 'Password:',
  okText: 'OK',
  cancelText: 'Cancel',
  showCancel: true
});

const emit = defineEmits<{
  (e: 'submit', credentials: LoginCredentials): void;
  (e: 'cancel'): void;
}>();

const username = ref<string>('');
const password = ref<string>('');

const handleSubmit = (): void => {
  emit('submit', { 
    username: username.value, 
    password: password.value 
  });
};

const handleCancel = (): void => {
  emit('cancel');
};
</script>

<template>
  <div class="wt-dialog">
    <div class="wt-title">{{ title }}</div>
    
    <div class="wt-body">
      <div class="wt-field-group">
        <label class="wt-label">{{ usernameLabel }}</label>
        <input 
          v-model="username" 
          type="text" 
          class="wt-input" 
          placeholder="Enter username"
          @keyup.enter="handleSubmit"
        />
      </div>

      <div class="wt-field-group">
        <label class="wt-label">{{ passwordLabel }}</label>
        <input 
          v-model="password" 
          type="password" 
          class="wt-input" 
          placeholder="Enter password"
          @keyup.enter="handleSubmit"
        />
      </div>
    </div>

    <div class="wt-footer">
      <button 
        type="button" 
        class="wt-btn wt-btn-ok" 
        @click="handleSubmit"
      >
        {{ okText }}
      </button>
      <button 
        v-if="showCancel" 
        type="button" 
        class="wt-btn wt-btn-cancel" 
        @click="handleCancel"
      >
        {{ cancelText }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.wt-dialog {
  background-color: #1b1b1b;
  border: 2px solid #c0c0c0;
  box-shadow: 0 0 0 1px #000, 0 0 10px #000;
  margin: 20px auto;
  max-width: 500px;
  font-family: "DejaVu Sans Mono", "Courier New", monospace;
  color: #e0e0e0;
}

.wt-title {
  background-color: #005f87;
  color: #fff;
  padding: 6px 10px;
  font-weight: bold;
  border-bottom: 1px solid #000;
}

.wt-body {
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.wt-field-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.wt-label {
  font-size: 0.9rem;
}

.wt-input {
  width: 100%;
  padding: 6px 8px;
  background-color: #000;
  border: 1px solid #5f5f5f;
  color: #e0e0e0;
  font-family: inherit;
}

.wt-input:focus {
  outline: none;
  border-color: #ffff00;
}

.wt-footer {
  padding: 8px 10px;
  border-top: 1px solid #000;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  background-color: #1b1b1b;
}

.wt-btn {
  min-width: 80px;
  padding: 4px 12px;
  border: 1px solid #c0c0c0;
  background-color: #3a3a3a;
  color: #fff;
  cursor: pointer;
  text-align: center;
  font-family: inherit;
}

.wt-btn:hover {
  background-color: #4f4f4f;
}

.wt-btn:active {
  background-color: #2a2a2a;
}

.wt-btn-ok {
  border-color: #00ff00;
}

.wt-btn-cancel {
  border-color: #ff0000;
}
</style>
