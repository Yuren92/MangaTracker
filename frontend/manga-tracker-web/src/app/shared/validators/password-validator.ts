import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Same rules as PasswordValidator in the API, checked here so the user sees what is
// missing before sending the form. The API still enforces them.
export const PASSWORD_RULES_HINT = 'Mínimo 8 caracteres, con mayúscula, minúscula y número';

export const strongPasswordValidator: ValidatorFn = (
  control: AbstractControl<string>
): ValidationErrors | null => {
  const value = control.value ?? '';

  if (!value) {
    return null;
  }

  const meetsRules =
    value.length >= 8 &&
    /\p{Lu}/u.test(value) &&
    /\p{Ll}/u.test(value) &&
    /\p{Nd}/u.test(value);

  return meetsRules ? null : { weakPassword: true };
};
