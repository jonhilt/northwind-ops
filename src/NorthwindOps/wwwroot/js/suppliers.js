// Browser-side rules for the Suppliers screen's static SSR form. Loaded by wwwroot/NorthwindOps.lib.module.js.

// xray:ssr-client-validation
// The client half of [SupplierCode]: the server emits a "suppliercode" rule with these params,
// and blazor.web.js calls this validator when the field changes and before the form submits.
export function registerSupplierValidators(formValidation) {
    formValidation.addValidator('suppliercode', ({ value, params }) => {
        if (!value) {
            return { success: true }; // [Required] reports empty fields
        }

        const letters = Number(params.letters), digits = Number(params.digits);
        const pattern = new RegExp(`^[A-Z]{${letters}}-[0-9]{${digits}}$`);
        return { success: pattern.test(value) && !value.endsWith('0'.repeat(digits)) };
    });
}
// /xray:ssr-client-validation
