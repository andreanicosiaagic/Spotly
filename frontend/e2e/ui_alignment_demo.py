from pathlib import Path
from tempfile import gettempdir

from playwright.sync_api import sync_playwright


routes = [
    ("dashboard", "/", "La mia giornata"),
    ("parking", "/parking", "Parcheggio"),
    ("desk", "/desk", "Postazioni"),
    ("lunch", "/lunch", "Pranzo"),
]

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    for width, height, label in [(430, 932, "mobile"), (1440, 980, "desktop")]:
        page = browser.new_page(viewport={"width": width, "height": height})
        for name, route, heading in routes:
            page.goto(f"http://127.0.0.1:5173{route}")
            page.wait_for_load_state("load")
            page.locator("h1:visible").first.wait_for()
            assert heading in page.content()
            logo = page.locator('img[alt="Spotly"]:visible').first
            logo.wait_for()
            assert logo.evaluate("img => img.naturalWidth > 0")
            assert page.evaluate("document.documentElement.scrollWidth <= document.documentElement.clientWidth")
            page.screenshot(path=str(Path(gettempdir()) / f"spotly-{label}-{name}.png"), full_page=True)
        page.close()
    browser.close()
