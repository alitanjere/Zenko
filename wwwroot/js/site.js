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

    const fileInput = document.getElementById('archivosExcel');
    const dropZone = document.getElementById('upload-dropzone');
    const fileList = document.getElementById('file-list');
    const fileSummary = document.getElementById('file-summary');
    const clearButton = document.getElementById('clear-files');

    if (fileInput && dropZone && fileList && fileSummary && clearButton) {
        const formatBytes = (bytes) => {
            if (!bytes) {
                return '0 KB';
            }
            const units = ['KB', 'MB', 'GB'];
            let size = bytes / 1024;
            let unitIndex = 0;

            while (size >= 1024 && unitIndex < units.length - 1) {
                size /= 1024;
                unitIndex += 1;
            }

            const decimals = size >= 10 || unitIndex === 0 ? 0 : 1;
            return `${size.toFixed(decimals)} ${units[unitIndex]}`;
        };

        const renderFiles = () => {
            const files = Array.from(fileInput.files || []);
            fileList.innerHTML = '';

            if (!files.length) {
                dropZone.classList.remove('has-files');
                fileList.innerHTML = '<li class="file-list__empty">Todavía no seleccionaste archivos. Arrastrá un Excel o hacé clic en el recuadro.</li>';
                fileSummary.textContent = 'Podés cargar tantos archivos como necesites en una misma tanda.';
                clearButton.disabled = true;
                return;
            }

            dropZone.classList.add('has-files');
            clearButton.disabled = false;

            let totalSize = 0;

            files.forEach((file, index) => {
                totalSize += file.size;

                const item = document.createElement('li');
                item.className = 'file-list__item';

                const info = document.createElement('div');
                info.className = 'file-list__info';

                const name = document.createElement('span');
                name.className = 'file-name';
                name.textContent = file.name;

                const meta = document.createElement('span');
                meta.className = 'file-meta';
                meta.textContent = formatBytes(file.size);

                info.appendChild(name);
                info.appendChild(meta);

                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'btn btn-link btn-sm text-danger p-0 ms-2 remove-file';
                removeButton.innerHTML = '<span aria-hidden="true">&times;</span><span class="visually-hidden">Quitar archivo</span>';
                removeButton.addEventListener('click', () => {
                    if (typeof DataTransfer === 'undefined') {
                        fileInput.value = '';
                        renderFiles();
                        return;
                    }

                    const dataTransfer = new DataTransfer();
                    Array.from(fileInput.files).forEach((existingFile, existingIndex) => {
                        if (existingIndex !== index) {
                            dataTransfer.items.add(existingFile);
                        }
                    });
                    fileInput.files = dataTransfer.files;
                    renderFiles();
                });

                item.appendChild(info);
                item.appendChild(removeButton);
                fileList.appendChild(item);
            });

            const summaryText = files.length === 1
                ? '1 archivo listo para procesar'
                : `${files.length} archivos listos para procesar`;
            fileSummary.textContent = `${summaryText} • ${formatBytes(totalSize)}`;
        };

        fileInput.addEventListener('change', () => renderFiles());

        clearButton.addEventListener('click', () => {
            fileInput.value = '';
            renderFiles();
            fileInput.dispatchEvent(new Event('change'));
        });

        const preventDefaults = (event) => {
            event.preventDefault();
            event.stopPropagation();
        };

        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, (event) => {
                preventDefaults(event);
                dropZone.classList.add('is-dragover');
            });
        });

        ['dragleave', 'dragend'].forEach(eventName => {
            dropZone.addEventListener(eventName, (event) => {
                preventDefaults(event);
                if (event.type === 'dragleave' && event.relatedTarget && dropZone.contains(event.relatedTarget)) {
                    return;
                }
                dropZone.classList.remove('is-dragover');
            });
        });

        dropZone.addEventListener('drop', (event) => {
            preventDefaults(event);
            dropZone.classList.remove('is-dragover');
            if (event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files.length > 0) {
                fileInput.files = event.dataTransfer.files;
                fileInput.dispatchEvent(new Event('change'));
            }
        });

        renderFiles();
    }
});
