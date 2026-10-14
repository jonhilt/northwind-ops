// Blazor JS initializer: blazor.web.js imports this file on every full page load and calls its callbacks.
// Keep it a thin list of screen hooks so each screen's script stays in its own file under js/.
import { registerSupplierValidators } from './js/suppliers.js';

export function afterWebStarted(blazor) {
    // From RC2, blazor.web.js creates Blazor.formValidation as it starts, on every page, so registering
    // once here covers pages reached later by enhanced navigation too.
    registerSupplierValidators(blazor.formValidation);
}
