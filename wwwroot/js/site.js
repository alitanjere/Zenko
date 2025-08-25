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

    const slider = document.getElementById('featuredCarousel');
    if (slider) {
        new bootstrap.Carousel(slider, {
            interval: 3000,
            ride: 'carousel',
            pause: false,
            touch: true
        });
    }
});
