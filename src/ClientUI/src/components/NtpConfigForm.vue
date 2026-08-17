<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
    ntp: any
}>()

const newServer = ref('')

function addServer() {
    if (newServer.value.trim()) {
        props.ntp.Upstream.Servers.push(newServer.value.trim())
        newServer.value = ''
    }
}

function removeServer(index: number) {
    props.ntp.Upstream.Servers.splice(index, 1)
}
</script>

<template>
    <div class="form-section">
        <h3>NTP Server</h3>
        <div class="field checkbox">
            <label><input v-model="ntp.Enabled" type="checkbox" /> Enable NTP Service</label>
        </div>

        <div class="grid-2">
            <div class="field">
                <label>Listen Address</label>
                <input v-model="ntp.ListenAddress" type="text" class="input" />
            </div>
            <div class="field">
                <label>Port</label>
                <input v-model.number="ntp.Port" type="number" class="input" />
            </div>
            <div class="field">
                <label>Stratum</label>
                <input v-model.number="ntp.Stratum" type="number" class="input" />
            </div>
            <div class="field">
                <label>Reference ID</label>
                <input v-model="ntp.ReferenceId" type="text" class="input" />
            </div>
        </div>

        <h3>Upstream Time Synchronization</h3>
        <div class="field checkbox">
            <label
                ><input v-model="ntp.Upstream.Enabled" type="checkbox" /> Enable Upstream
                Sync</label
            >
        </div>

        <ul class="list">
            <li v-for="(server, idx) in ntp.Upstream.Servers" :key="idx">
                <span>{{ server }}</span>
                <button class="btn-sm btn-danger" @click="removeServer(Number(idx))">Remove</button>
            </li>
        </ul>
        <div class="input-group">
            <input v-model="newServer" placeholder="time.google.com" class="input" />
            <button class="btn-sm btn-primary" @click="addServer">Add Server</button>
        </div>
    </div>
</template>

<style scoped>
.form-section {
    display: flex;
    flex-direction: column;
    gap: 16px;
}
.grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}
.field {
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.field label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #475569;
}
.input {
    padding: 8px;
    border: 1px solid #cbd5e1;
    border-radius: 4px;
}
.list {
    list-style: none;
    padding: 0;
    margin: 0;
}
.list li {
    display: flex;
    justify-content: space-between;
    padding: 6px 0;
    border-bottom: 1px solid #f1f5f9;
}
.input-group {
    display: flex;
    gap: 8px;
}
.btn-sm {
    padding: 4px 8px;
    font-size: 0.8rem;
    border-radius: 4px;
    cursor: pointer;
    border: none;
}
.btn-primary {
    background: #2563eb;
    color: white;
}
.btn-danger {
    background: #ef4444;
    color: white;
}
</style>
