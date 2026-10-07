import { FormControl } from '@angular/forms';

import { strongPasswordValidator } from './password-validator';

describe('strongPasswordValidator', () => {
  const validate = (value: string) => strongPasswordValidator(new FormControl(value));

  it('accepts a password that follows every rule', () => {
    expect(validate('Password123')).toBeNull();
  });

  it('leaves empty values to Validators.required', () => {
    expect(validate('')).toBeNull();
  });

  it.each([
    ['too short', 'Pass1'],
    ['no uppercase', 'password123'],
    ['no lowercase', 'PASSWORD123'],
    ['no number', 'Passwordddd']
  ])('rejects a password with %s', (_rule, value) => {
    expect(validate(value)).toEqual({ weakPassword: true });
  });
});
