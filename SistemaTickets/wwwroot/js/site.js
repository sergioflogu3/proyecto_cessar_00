// site.js - comportamiento común (spec 003). Sin librerías ni paso de build.
(function () {
    'use strict';

    // ---- (a) Plegado de filas apiladas (RF-5) -------------------------------
    // Un único manejador delegado: .btn-plegar alterna .fila-expandida en su <tr>.
    document.addEventListener('click', function (e) {
        var btn = e.target.closest ? e.target.closest('.btn-plegar') : null;
        if (!btn) return;
        var fila = btn.closest('tr');
        if (!fila) return;
        var expandida = fila.classList.toggle('fila-expandida');
        btn.setAttribute('aria-expanded', expandida ? 'true' : 'false');
        btn.textContent = expandida ? 'Ver menos' : 'Ver más';
    });

    // ---- (b) Accesibilidad de los errores de validación (RF-26) -------------
    function campoDe(span) {
        var nombre = span.getAttribute('data-valmsg-for');
        if (!nombre) return null;
        var form = span.closest('form');
        return form.querySelector('[name="' + (window.CSS && CSS.escape ? CSS.escape(nombre) : nombre) + '"]');
    }

    function ids(valor) { return (valor || '').split(/\s+/).filter(Boolean); }

    function sincronizar() {
        document.querySelectorAll('span[data-valmsg-for]').forEach(function (span) {
            var campo = campoDe(span);
            if (!campo) return;
            if (!span.id) span.id = 'err-' + (campo.id || span.getAttribute('data-valmsg-for'));
            var lista = ids(campo.getAttribute('aria-describedby'));
            if (lista.indexOf(span.id) < 0) lista.push(span.id);
            campo.setAttribute('aria-describedby', lista.join(' '));
            var hayError = campo.classList.contains('input-validation-error') ||
                (span.classList.contains('field-validation-error') && span.textContent.trim() !== '');
            campo.setAttribute('aria-invalid', hayError ? 'true' : 'false');
        });
    }

    var pendiente = false;
    function programar() {
        if (pendiente) return;
        pendiente = true;
        requestAnimationFrame(function () { pendiente = false; sincronizar(); });
    }

    // ---- (c) Validación mínima de formularios sin ViewModel (RF-26) ---------
    // <form data-validar> + data-msg-required="..." en el formulario o en cada campo.
    function mensajeDe(campo, form, clave) {
        return campo.getAttribute('data-msg-' + clave) || form.getAttribute('data-msg-' + clave) || campo.validationMessage;
    }

    function limpiarError(campo) {
        var id = campo.getAttribute('data-err-id');
        if (id) {
            var div = document.getElementById(id);
            if (div) div.remove();
            var lista = ids(campo.getAttribute('aria-describedby')).filter(function (x) { return x !== id; });
            if (lista.length) campo.setAttribute('aria-describedby', lista.join(' '));
            else campo.removeAttribute('aria-describedby');
            campo.removeAttribute('data-err-id');
        }
        campo.classList.remove('is-invalid');
        campo.removeAttribute('aria-invalid');
    }

    var contador = 0;
    function mostrarError(campo, texto) {
        limpiarError(campo);
        var id = 'err-v-' + (campo.id || campo.name || 'campo') + '-' + (++contador);
        var div = document.createElement('div');
        div.className = 'invalid-feedback d-block';
        div.id = id;
        div.textContent = texto;
        campo.insertAdjacentElement('afterend', div);
        campo.setAttribute('data-err-id', id);
        campo.classList.add('is-invalid');
        campo.setAttribute('aria-invalid', 'true');
        var lista = ids(campo.getAttribute('aria-describedby'));
        lista.push(id);
        campo.setAttribute('aria-describedby', lista.join(' '));
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form.matches || !form.matches('form[data-validar]')) return;
        var primero = null;
        form.querySelectorAll('input[required], select[required], textarea[required]').forEach(function (campo) {
            limpiarError(campo);
            if (!campo.value || !String(campo.value).trim()) {
                mostrarError(campo, mensajeDe(campo, form, 'required'));
                if (!primero) primero = campo;
            }
        });
        if (primero) {
            e.preventDefault();
            e.stopPropagation();
            primero.focus();
        }
    }, true);

    document.addEventListener('input', function (e) {
        var campo = e.target;
        if (campo.getAttribute && campo.getAttribute('data-err-id')) limpiarError(campo);
    });

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form[data-validar]').forEach(function (f) { f.setAttribute('novalidate', ''); });
        sincronizar();
        new MutationObserver(programar).observe(document.body, {
            subtree: true, childList: true, attributes: true, attributeFilter: ['class']
        });
    });
})();
