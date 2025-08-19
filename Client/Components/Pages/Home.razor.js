// wwwroot/js/carouselHeight.js
export function initCarouselAutoHeight(container, maxHeight = 500) {
    if (!container) return;

    const findActiveImage = () => {
        // Look for the currently active slide's image inside the provided container
        // MudCarousel typically toggles a class for active items; fall back to first image if needed
        const active = container.querySelector(".mud-carousel-item-active") || container.querySelector(".mud-carousel-item");
        if (!active) return null;
        return active.querySelector("img, picture img, .responsive-carousel-image");
    };

    const setHeightFromImage = (img) => {
        if (!img) return;
        // Use rendered height; if zero (not yet loaded), try natural ratio once loaded
        const h = img.clientHeight || img.naturalHeight || 0;
        if (h > 0) {
            container.style.height = Math.min(h, maxHeight) + "px";
        }
    };

    // Observe changes to which slide is active (class changes)
    const mutationObserver = new MutationObserver(() => {
        const img = findActiveImage();
        setHeightFromImage(img);
        if (img) {
            resizeObserver.disconnect();
            resizeObserver.observe(img);
        }
    });

    mutationObserver.observe(container, { subtree: true, attributes: true, attributeFilter: ["class"] });

    // Observe size changes of the active image
    const resizeObserver = new ResizeObserver(() => {
        const img = findActiveImage();
        setHeightFromImage(img);
    });

    // Also respond to image load events
    const initImageWatch = () => {
        const img = findActiveImage();
        if (!img) return;
        if (!img.complete) {
            img.addEventListener("load", () => setHeightFromImage(img), { once: true });
        }
        resizeObserver.observe(img);
        setHeightFromImage(img);
    };

    initImageWatch();
}