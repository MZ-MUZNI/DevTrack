// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
    const root = document.documentElement;
    const toggle = document.getElementById("themeToggle");

    if (!toggle) {
        return;
    }

    const updateThemeToggle = () => {
        const lightMode = root.classList.contains("theme-light");
        toggle.setAttribute("aria-pressed", String(lightMode));
        toggle.setAttribute("aria-label", lightMode ? "Switch to dark mode" : "Switch to light mode");
        toggle.querySelector(".theme-toggle-icon").textContent = lightMode ? "\u263E" : "\u2600";
        toggle.querySelector(".theme-toggle-label").textContent = lightMode ? "Dark mode" : "Light mode";
    };

    updateThemeToggle();

    toggle.addEventListener("click", () => {
        const lightMode = root.classList.toggle("theme-light");
        localStorage.setItem("devtrack-theme", lightMode ? "light" : "dark");
        updateThemeToggle();
    });
})();
