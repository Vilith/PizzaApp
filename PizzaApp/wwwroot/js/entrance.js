window.pizzaEntrance = (() => {
    let stopCurrent = () => {};
    return { play(destination) {
    stopCurrent();
    const entrance = document.getElementById('entrance');
    const app = document.getElementById('app');
    const skip = document.getElementById('entrance-skip');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    if (!entrance || !app || !skip || reducedMotion.matches) return;

    const restaurant = destination === 'Pizza' || destination === 'Sperring' ? destination : null;
    const cleanupJourney = restaurant ? createDoorJourney(entrance, app, restaurant) : () => {};
    entrance.querySelector('.entrance-window span').textContent = restaurant || 'Välkommen in';
    entrance.querySelector('.entrance-caption').textContent = restaurant
        ? `Välkommen in till ${restaurant}.` : 'Välkommen in till dagens lunch.';

    let finished = false;
    let timer;
    const previousFocus = document.activeElement;
    stopCurrent = finish;

    function finish() {
        if (finished) return;
        finished = true;
        clearTimeout(timer);
        reducedMotion.removeEventListener('change', motionChanged);
        document.removeEventListener('keydown', onKeyDown);
        const restoreFocus = entrance.contains(document.activeElement);
        entrance.hidden = true;
        entrance.classList.remove('entrance-playing');
        cleanupJourney();
        app.inert = false;
        document.documentElement.classList.remove('entrance-running');
        if (restoreFocus) {
            const target = previousFocus && previousFocus !== document.body && previousFocus.isConnected
                ? previousFocus : app.querySelector('h1, input, button, a');
            if (target) {
                if (!target.matches('input, button, a, [tabindex]')) target.tabIndex = -1;
                target.focus({ preventScroll: true });
            }
        }
    }

    function motionChanged(event) { if (event.matches) finish(); }
    function onKeyDown(event) { if (event.key === 'Escape') finish(); }

    skip.onclick = finish;
    entrance.onanimationend = event => {
        if (event.target === entrance && event.animationName === 'entrance-reveal') finish();
    };
    reducedMotion.addEventListener('change', motionChanged);
    document.addEventListener('keydown', onKeyDown);
    // Also finish if animation events are unavailable or CSS fails to load.
    timer = setTimeout(finish, 5300);
    entrance.hidden = false;
    // Restart the scene even if a previous animation was just skipped.
    void entrance.offsetWidth;
    app.inert = true;
    document.documentElement.classList.add('entrance-running');
    skip.focus({ preventScroll: true });
    entrance.classList.add('entrance-playing');
    } };

    function createDoorJourney(entrance, app, restaurant) {
        const shell = app.querySelector('.app-shell');
        const doors = [...app.querySelectorAll('.restaurant-lobby .restaurant-card')];
        const selectedIndex = doors.findIndex(door => door.querySelector('strong')?.textContent.trim() === restaurant);
        if (!shell || selectedIndex < 0) return () => {};

        // Preserve the actual layout, including both doors and the current scroll position.
        // Blazor can load the destination beneath this inert visual copy.
        const shellBounds = shell.getBoundingClientRect();
        const doorBounds = doors[selectedIndex].getBoundingClientRect();
        const surfaceStyle = getComputedStyle(doors[selectedIndex]);
        const scene = shell.cloneNode(true);
        scene.classList.add('door-journey');
        scene.setAttribute('aria-hidden', 'true');
        scene.inert = true;
        scene.removeAttribute('id');
        scene.querySelectorAll('[id]').forEach(element => element.removeAttribute('id'));
        Object.assign(scene.style, {
            position: 'absolute', left: `${shellBounds.left}px`, top: `${shellBounds.top}px`,
            width: `${shellBounds.width}px`, height: `${shellBounds.height}px`,
            margin: '0', pointerEvents: 'none', fontFamily: getComputedStyle(shell).fontFamily,
            transformOrigin: `${doorBounds.left + doorBounds.width / 2 - shellBounds.left}px ${doorBounds.top + doorBounds.height / 2 - shellBounds.top}px`
        });

        const selected = scene.querySelectorAll('.restaurant-card')[selectedIndex];
        const leaf = document.createElement('div');
        leaf.className = 'door-journey-leaf';
        leaf.style.background = surfaceStyle.background;
        leaf.style.boxShadow = 'inset 0 0 0 5px #1a4036';
        while (selected.firstChild) leaf.appendChild(selected.firstChild);
        selected.appendChild(leaf);
        selected.style.background = '#f5f8fc';
        selected.style.perspective = '1000px';
        selected.style.transform = 'none';
        selected.style.filter = 'none';
        entrance.appendChild(scene);
        entrance.classList.add('entrance-journey');

        const dx = innerWidth / 2 - (doorBounds.left + doorBounds.width / 2);
        const dy = innerHeight / 2 - (doorBounds.top + doorBounds.height / 2);
        const zoom = Math.max(innerWidth / doorBounds.width, innerHeight / doorBounds.height) * 1.6;
        // First approach the chosen door; only then open it and walk through.
        const camera = scene.animate([
            { transform: 'translate(0, 0) scale(1)', offset: 0 },
            { transform: `translate(${dx}px, ${dy}px) scale(1.12)`, offset: .36 },
            { transform: `translate(${dx}px, ${dy}px) scale(1.16)`, offset: .56 },
            { transform: `translate(${dx}px, ${dy}px) scale(${zoom})`, offset: 1 }
        ], { duration: 4700, easing: 'ease-in-out', fill: 'both' });
        const opening = leaf.animate([
            { transform: 'rotateY(0deg)' }, { transform: 'rotateY(-105deg)' }
        ], { duration: 1600, delay: 1750, easing: 'cubic-bezier(.3, 0, .2, 1)', fill: 'both' });

        return () => {
            camera.cancel();
            opening.cancel();
            scene.remove();
            entrance.classList.remove('entrance-journey');
        };
    }
})();

window.pizzaPage = {
    center(id) {
        const target = document.getElementById(id);
        if (!target) return;
        if (!target.hasAttribute('tabindex')) target.setAttribute('tabindex', '-1');
        target.focus({ preventScroll: true });
        target.scrollIntoView({
            behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth',
            block: 'center'
        });
    }
};
