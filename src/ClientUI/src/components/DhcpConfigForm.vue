<script setup lang="ts">
defineProps<{
    dhcp: any
}>()
</script>

<template>
    <div class="form-section">
        <h3>DHCP Settings</h3>
        <div class="field checkbox">
            <label><input v-model="dhcp.Enabled" type="checkbox" /> Enable DHCP Service</label>
        </div>

        <div class="grid-2">
            <div class="field">
                <label>Listen Address</label>
                <input v-model="dhcp.ListenAddress" type="text" class="input" />
            </div>
            <div class="field">
                <label>Listen Port</label>
                <input v-model.number="dhcp.ListenPort" type="number" class="input" />
            </div>
            <div class="field">
                <label>Pool CIDR</label>
                <input v-model="dhcp.PoolCidr" type="text" class="input" />
            </div>
            <div class="field">
                <label>Server Identifier</label>
                <input v-model="dhcp.ServerIdentifier" type="text" class="input" />
            </div>
            <div class="field">
                <label>Router (Gateway)</label>
                <input v-model="dhcp.Router" type="text" class="input" />
            </div>
            <div class="field">
                <label>DNS Server</label>
                <input v-model="dhcp.DnsServer" type="text" class="input" />
            </div>
            <div class="field">
                <label>Domain Name</label>
                <input v-model="dhcp.DomainName" type="text" class="input" />
            </div>
            <div class="field">
                <label>Lease Duration (Hours)</label>
                <input v-model.number="dhcp.LeaseHours" type="number" class="input" />
            </div>
        </div>

        <h3>PXE Boot & Proxy Options</h3>
        <div class="grid-2">
            <div class="field">
                <label>TFTP Server Name</label>
                <input v-model="dhcp.TftpServerName" type="text" class="input" />
            </div>
            <div class="field">
                <label>Bootfile Name</label>
                <input v-model="dhcp.BootfileName" type="text" class="input" />
            </div>
            <div class="field">
                <label>WPAD Web Proxy URL</label>
                <input v-model="dhcp.WebProxyServerUrl" type="text" class="input" />
            </div>
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
</style>
