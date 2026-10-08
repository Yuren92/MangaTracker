import { smallCover } from './cover.pipe';

describe('smallCover', () => {
  const path = '0/308/90420-18166-106379-1-walking-dead.jpg';

  it.each(['original', 'scale_medium', 'scale_large', 'screen_kubrick', 'screen_medium'])(
    'turns the %s rendition into scale_small',
    rendition => {
      expect(smallCover(`https://comicvine.gamespot.com/a/uploads/${rendition}/${path}`))
        .toBe(`https://comicvine.gamespot.com/a/uploads/scale_small/${path}`);
    }
  );

  it('leaves small covers and other URLs alone', () => {
    const small = `https://comicvine.gamespot.com/a/uploads/scale_small/${path}`;
    expect(smallCover(small)).toBe(small);
    expect(smallCover('https://example.com/cover.jpg')).toBe('https://example.com/cover.jpg');
  });

  it('keeps a missing cover missing', () => {
    expect(smallCover(null)).toBeNull();
  });
});
