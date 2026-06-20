from pathlib import Path
from tempfile import gettempdir

from playwright.sync_api import sync_playwright


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 430, "height": 932})
    page.goto("http://127.0.0.1:5173/")
    page.wait_for_load_state("load")
    panel = page.get_by_role("complementary", name="Team Product")
    panel.wait_for()
    assert "2" in panel.inner_text()
    panel.get_by_text("Giulia Romano").wait_for()
    panel.get_by_text("Paolo Riva").wait_for()
    assert "sede impostata su Teams" in panel.inner_text()
    page.screenshot(path=str(Path(gettempdir()) / "spotly-team-availability.png"), full_page=True)
    browser.close()
