// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Account dropdown functionality with animation and accessibility
document.addEventListener('DOMContentLoaded', function() {
    const accountDropdownButton = document.getElementById('accountDropdownButton');
    const accountDropdownMenu = document.getElementById('accountDropdownMenu');
    
    // Helper for animation
    function showDropdown(dropdown) {
        dropdown.classList.remove('hidden');
        dropdown.classList.remove('opacity-0', 'translate-y-2', 'pointer-events-none');
        dropdown.classList.add('opacity-100', 'translate-y-0');
        dropdown.setAttribute('aria-expanded', 'true');
    }
    function hideDropdown(dropdown) {
        dropdown.classList.remove('opacity-100', 'translate-y-0');
        dropdown.classList.add('opacity-0', 'translate-y-2', 'pointer-events-none');
        dropdown.setAttribute('aria-expanded', 'false');
        setTimeout(() => {
            dropdown.classList.add('hidden');
        }, 200); // match transition duration
    }

    if (accountDropdownButton && accountDropdownMenu) {
        // Initial state for animation
        accountDropdownMenu.classList.add('transition-all', 'duration-200', 'ease-out', 'opacity-0', 'translate-y-2', 'pointer-events-none', 'hidden');
        accountDropdownButton.setAttribute('aria-haspopup', 'true');
        accountDropdownButton.setAttribute('aria-expanded', 'false');
        accountDropdownButton.setAttribute('tabindex', '0');
        accountDropdownMenu.setAttribute('role', 'menu');
        accountDropdownMenu.setAttribute('aria-hidden', 'true');

        let dropdownOpen = false;

        function toggleDropdown(e) {
            e.preventDefault();
            if (dropdownOpen) {
                hideDropdown(accountDropdownMenu);
                dropdownOpen = false;
                accountDropdownMenu.setAttribute('aria-hidden', 'true');
            } else {
                showDropdown(accountDropdownMenu);
                dropdownOpen = true;
                accountDropdownMenu.setAttribute('aria-hidden', 'false');
            }
        }

        accountDropdownButton.addEventListener('click', toggleDropdown);

        // Keyboard accessibility
        accountDropdownButton.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' || e.key === ' ') {
                toggleDropdown(e);
            }
            if (e.key === 'Escape' && dropdownOpen) {
                hideDropdown(accountDropdownMenu);
                dropdownOpen = false;
                accountDropdownMenu.setAttribute('aria-hidden', 'true');
            }
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (
                dropdownOpen &&
                !accountDropdownButton.contains(e.target) &&
                !accountDropdownMenu.contains(e.target)
            ) {
                hideDropdown(accountDropdownMenu);
                dropdownOpen = false;
                accountDropdownMenu.setAttribute('aria-hidden', 'true');
            }
        });
    }
    
    // Mobile menu functionality
    const mobileMenuToggle = document.getElementById('mobileMenuToggle');
    const mobileMenu = document.getElementById('mobileMenu');
    
    if (mobileMenuToggle && mobileMenu) {
        mobileMenuToggle.addEventListener('click', function() {
            if (mobileMenu.classList.contains('-translate-y-full')) {
                mobileMenu.classList.remove('-translate-y-full');
            } else {
                mobileMenu.classList.add('-translate-y-full');
            }
        });
    }
});
