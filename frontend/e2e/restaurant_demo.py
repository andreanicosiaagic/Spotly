from pathlib import Path
from tempfile import gettempdir

from playwright.sync_api import sync_playwright


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 430, "height": 932})
    page.goto("http://127.0.0.1:5173/lunch")
    # This page intentionally polls every three seconds in demo mode, so networkidle is unreachable.
    page.wait_for_load_state("load")

    page.get_by_role("heading", name="Posti comunicati dai locali").wait_for()
    bistrot = page.get_by_role("article").filter(has_text="Bistrot Verde")
    initial_seats = int(bistrot.locator("strong").inner_text())

    page.get_by_role("button", name="Ricevi aggiornamenti demo").click()
    page.wait_for_function(
        "([selector, initial]) => Number(document.querySelector(selector)?.textContent) !== initial",
        arg=["article strong", initial_seats],
    )
    updated_seats = int(bistrot.locator("strong").inner_text())
    assert updated_seats != initial_seats
    page.get_by_text("disponibilità aggiornata", exact=False).first.wait_for()

    bistrot.get_by_role("button", name="Prenota 1 posto").click()
    confirmation = page.get_by_role("status")
    confirmation.get_by_text("Conferma locale · OK").wait_for()
    assert "Restano" in confirmation.inner_text()
    assert int(bistrot.locator("strong").inner_text()) == updated_seats - 1

    page.screenshot(path=str(Path(gettempdir()) / "spotly-restaurant-demo.png"), full_page=True)
    browser.close()
