const API = {
    async get(url) {
        const resp = await fetch(url);
        if (!resp.ok) throw new Error(`GET ${url}: ${resp.status}`);
        return resp.json();
    },
    async put(url, data) {
        const resp = await fetch(url, {
            method: 'PUT',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify(data),
        });
        if (!resp.ok) throw new Error(`PUT ${url}: ${resp.status}`);
        return resp.json();
    },
    async post(url, data = null) {
        const opts = {method: 'POST', headers: {'Content-Type': 'application/json'}};
        if (data !== null) opts.body = JSON.stringify(data);
        const resp = await fetch(url, opts);
        if (!resp.ok) throw new Error(`POST ${url}: ${resp.status}`);
        return resp.json();
    },
    async del(url) {
        const resp = await fetch(url, {method: 'DELETE'});
        if (!resp.ok) throw new Error(`DELETE ${url}: ${resp.status}`);
        return resp.json();
    }
};

function showToast(message, type = 'info') {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}

function initTabs() {
    document.querySelectorAll('.tabs').forEach(tabBar => {
        tabBar.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                const target = tab.dataset.tab;
                tabBar.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                const parent = tabBar.parentElement;
                parent.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
                const content = parent.querySelector(`#${target}`);
                if (content) content.classList.add('active');
            });
        });
    });
}

function setActiveNav() {
    const path = window.location.pathname;
    document.querySelectorAll('.nav-link').forEach(link => {
        link.classList.remove('active');
        if (link.getAttribute('href') === path) {
            link.classList.add('active');
        }
    });
}

function formatUptime(seconds) {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    return `${h}h ${m}m`;
}

async function updateServerStatus() {
    try {
        const state = await API.get('/api/server/state');
        const dot = document.querySelector('.status-dot');
        const label = document.querySelector('.status-label');
        if (dot && label) {
            dot.classList.toggle('online', state.is_running);
            label.textContent = state.is_running
                ? `Online - ${state.players_online}/${state.max_players}`
                : 'Offline';
        }
    } catch {
        // Admin plugin not reachable
    }
}

document.addEventListener('DOMContentLoaded', () => {
    setActiveNav();
    initTabs();
    updateServerStatus();
    setInterval(updateServerStatus, 10000);
});
