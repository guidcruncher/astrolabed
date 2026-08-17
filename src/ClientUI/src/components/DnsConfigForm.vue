<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
    dns: any
}>()

const newHostFile = ref('')

function addHostFile() {
    if (newHostFile.value.trim()) {
        props.dns.HostsFiles.push(newHostFile.value.trim())
        newHostFile.value = ''
    }
}

function removeHostFile(index: number) {
    props.dns.HostsFiles.splice(index, 1)
}

function addResolver() {
    props.dns.Resolvers.push({ Name: 'New Resolver', Rule: '', Block: false, Port: 53 })
}

function removeResolver(index: number) {
    props.dns.Resolvers.splice(index, 1)
}
</script>

<template>
    <div class="form-section">
        <h3>DNS Core & Socket</h3>
        <div class="grid-2">
            <div class="field">
                <label>Listen Address</label>
                <input v-model="dns.Listen.Address" type="text" class="input" />
            </div>
            <div class="field">
                <label>Listen Port</label>
                <input v-model.number="dns.Listen.Port" type="number" class="input" />
            </div>
            <div class="field">
                <label>Upstream Timeout (ms)</label>
                <input v-model.number="dns.UpstreamTimeoutMs" type="number" class="input" />
            </div>
        </div>

        <h3>Caching Configuration</h3>
        <div class="grid-2">
            <div class="field checkbox">
                <label
                    ><input v-model="dns.Caching.Enabled" type="checkbox" /> Enable Caching</label
                >
            </div>
            <div class="field">
                <label>TTL Seconds</label>
                <input v-model.number="dns.Caching.TtlSeconds" type="number" class="input" />
            </div>
            <div class="field">
                <label>Max Entries</label>
                <input v-model.number="dns.Caching.MaxEntries" type="number" class="input" />
            </div>
            <div class="field">
                <label>Cleanup Interval (Minutes)</label>
                <input
                    v-model.number="dns.Caching.CleanupIntervalMinutes"
                    type="number"
                    class="input"
                />
            </div>
        </div>

        <h3>Block Response Policy</h3>
        <div class="grid-3">
            <div class="field">
                <label>Mode</label>
                <select v-model="dns.BlockResponse.Mode" class="input">
                    <option value="NXDOMAIN">NXDOMAIN</option>
                    <option value="REFUSED">REFUSED</option>
                    <option value="StaticIp">StaticIp</option>
                </select>
            </div>
            <div class="field">
                <label>Static IP</label>
                <input v-model="dns.BlockResponse.StaticIp" type="text" class="input" />
            </div>
            <div class="field">
                <label>TTL (Seconds)</label>
                <input v-model.number="dns.BlockResponse.Ttl" type="number" class="input" />
            </div>
        </div>

        <h3>Custom Resolvers</h3>
        <div v-for="(resolver, index) in dns.Resolvers" :key="index" class="resolver-item">
            <div class="grid-3">
                <input v-model="resolver.Name" placeholder="Name" class="input" />
                <input v-model="resolver.Rule" placeholder="Regex Rule" class="input" />
                <input v-model="resolver.Address" placeholder="Target Address" class="input" />
            </div>
            <div class="resolver-footer">
                <label><input v-model="resolver.Block" type="checkbox" /> Block Match</label>
                <button class="btn-sm btn-danger" @click="removeResolver(Number(index))">
                    Remove
                </button>
            </div>
        </div>
        <button class="btn-sm btn-secondary" @click="addResolver">+ Add Resolver</button>

        <h3>Hosts Files</h3>
        <ul class="list">
            <li v-for="(file, idx) in dns.HostsFiles" :key="idx">
                <span>{{ file }}</span>
                <button class="btn-sm btn-danger" @click="removeHostFile(Number(idx))">
                    Remove
                </button>
            </li>
        </ul>
        <div class="input-group">
            <input v-model="newHostFile" placeholder="file:///path/to/hosts" class="input" />
            <button class="btn-sm btn-primary" @click="addHostFile">Add Path</button>
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
.grid-3 {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
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
.resolver-item {
    border: 1px solid #e2e8f0;
    padding: 12px;
    border-radius: 6px;
    background: #f8fafc;
    margin-bottom: 8px;
}
.resolver-footer {
    display: flex;
    justify-content: space-between;
    margin-top: 8px;
    align-items: center;
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
.btn-secondary {
    background: #e2e8f0;
    color: #334155;
}
.btn-danger {
    background: #ef4444;
    color: white;
}
</style>
