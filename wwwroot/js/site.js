// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    const sortButton = document.getElementById('sort-button');
    if (sortButton) {
        // Initial state from the button text
        let isAscending = !sortButton.textContent.includes('Z-A');

        sortButton.addEventListener('click', function () {
            const container = document.getElementById('productos-container');
            const productCards = Array.from(container.querySelectorAll('.producto-card'));

            productCards.sort((a, b) => {
                const nameA = a.dataset.nombre.toLowerCase();
                const nameB = b.dataset.nombre.toLowerCase();

                if (nameA < nameB) {
                    return isAscending ? -1 : 1;
                }
                if (nameA > nameB) {
                    return isAscending ? 1 : -1;
                }
                return 0;
            });

            // Re-append sorted cards
            productCards.forEach(card => container.appendChild(card));

            // Toggle sort order and update button text
            isAscending = !isAscending;
            sortButton.textContent = isAscending ? 'Ordenar A-Z' : 'Ordenar Z-A';
        });
    }

    const themeToggle = document.getElementById('theme-toggle');
    const themeLabel = themeToggle ? themeToggle.querySelector('.theme-label') : null;
    if (themeToggle) {
        const body = document.body;
        const storedTheme = localStorage.getItem('theme') || 'light';
        body.setAttribute('data-theme', storedTheme);
        updateToggleAppearance(storedTheme);

        themeToggle.addEventListener('click', function () {
            const currentTheme = body.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
            body.setAttribute('data-theme', currentTheme);
            localStorage.setItem('theme', currentTheme);
            updateToggleAppearance(currentTheme);
        });
    }

    function updateToggleAppearance(theme) {
        if (themeToggle && themeLabel) {
            themeLabel.textContent = theme === 'dark' ? 'Modo claro' : 'Modo oscuro';
            themeToggle.setAttribute('aria-pressed', theme === 'dark');
        }
    }

    const header = document.querySelector('.navbar');
    if (header) {
        const handleScroll = () => {
            if (window.scrollY > 12) {
                header.classList.add('navbar-scrolled');
            } else {
                header.classList.remove('navbar-scrolled');
            }
        };
        handleScroll();
        window.addEventListener('scroll', handleScroll);
    }

    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId.length > 1) {
                const targetElement = document.querySelector(targetId);
                if (targetElement) {
                    e.preventDefault();
                    targetElement.scrollIntoView({ behavior: 'smooth' });
                }
            }
        });
    });
});
