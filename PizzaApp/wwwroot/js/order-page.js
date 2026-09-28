window.pizzaPage = {
    async center(id) {
        // Let the browser finish layout after Blazor replaces the empty selection.
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
        const target = document.getElementById(id);
        if (!target) return;
        try { target.focus({ preventScroll: true }); }
        catch { target.focus(); }
        const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
        try {
            target.scrollIntoView({
                behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth',
                block: target.getBoundingClientRect().height > viewportHeight - 32 ? 'start' : 'center'
            });
        } catch {
            // Older mobile browsers may only support the boolean overload.
            target.scrollIntoView(true);
        }
    }
};
