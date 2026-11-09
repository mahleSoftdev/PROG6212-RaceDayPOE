(function () {
    'use strict';

    // Confirm before destructive actions: <form data-confirm="Delete this event?">
    document.addEventListener('submit', function (e) {
        var message = e.target.getAttribute && e.target.getAttribute('data-confirm');
        if (message && !window.confirm(message)) e.preventDefault();
    });

    // Prevent double submits and show progress on the pressed button.
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (e.defaultPrevented) return;
            if (window.jQuery && window.jQuery(form).valid && !window.jQuery(form).valid()) return;
            var btn = form.querySelector('button[type=submit]:not([data-no-busy])');
            if (btn && !btn.disabled) {
                setTimeout(function () { btn.disabled = true; btn.classList.add('is-busy'); }, 0);
            }
        });
    });

    // Live image preview: <input type="file" data-preview="#target-img">
    document.querySelectorAll('input[type=file][data-preview]').forEach(function (input) {
        input.addEventListener('change', function () {
            var target = document.querySelector(input.getAttribute('data-preview'));
            var file = input.files && input.files[0];
            if (!target || !file) return;
            var reader = new FileReader();
            reader.onload = function (ev) { target.src = ev.target.result; target.closest('.preview-box').classList.remove('d-none'); };
            reader.readAsDataURL(file);
        });
    });

    // Character counter: <textarea data-counter="#count">
    document.querySelectorAll('textarea[data-counter]').forEach(function (area) {
        var out = document.querySelector(area.getAttribute('data-counter'));
        var update = function () { if (out) out.textContent = area.value.length; };
        area.addEventListener('input', update); update();
    });

    // Banner/avatar images that fail to load fall back to the gradient cover.
    document.querySelectorAll('img[data-fallback]').forEach(function (img) {
        img.addEventListener('error', function () { img.closest('.cover-wrap').classList.add('cover-empty'); img.remove(); });
    });
})();
