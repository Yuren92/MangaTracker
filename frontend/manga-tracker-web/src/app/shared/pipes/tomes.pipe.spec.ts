import { TomesPipe } from './tomes.pipe';

describe('TomesPipe', () => {
  const pipe = new TomesPipe();

  it('agrees in number', () => {
    expect(pipe.transform(1)).toBe('1 tomo');
    expect(pipe.transform(0)).toBe('0 tomos');
    expect(pipe.transform(14)).toBe('14 tomos');
  });

  it('says so when the count is unknown', () => {
    expect(pipe.transform(null)).toBe('? tomos');
  });
});
