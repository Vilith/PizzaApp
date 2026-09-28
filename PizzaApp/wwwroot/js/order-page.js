window.pizzaPage = {
    async center(id) {
        // Let the browser finish layout after Blazor replaces the empty selection.
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
        const target = document.getElementById(id);
        if (!target) return;
        target.focus({ preventScroll: true });
        const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
        target.scrollIntoView({
            behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth',
            block: target.getBoundingClientRect().height > viewportHeight - 32 ? 'start' : 'center'
        });
    }
};
