<script setup lang="ts"></script>

<template>
    <router-view />
</template>

<style scoped>
.demo-header {
    text-align: center;
    margin-bottom: 24px;
    font-family: 'DejaVu Sans Mono', 'Courier New', monospace;
}

.demo-header h1 {
    color: #ffff00;
    font-size: 1.5rem;
    margin-bottom: 6px;
}

.demo-header p {
    color: #a0a0a0;
    font-size: 0.9rem;
}

.console-dialog {
    max-width: 100% !important;
    margin-bottom: 24px !important;
}

.console-body {
    height: 120px;
    overflow-y: auto;
    background-color: #000;
    font-family: inherit;
    font-size: 0.85rem;
}

.console-empty {
    color: #666;
    font-style: italic;
}

.console-line {
    color: #00ff00;
    white-space: pre-wrap;
}

.grid-container {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    gap: 20px;
}
</style>
