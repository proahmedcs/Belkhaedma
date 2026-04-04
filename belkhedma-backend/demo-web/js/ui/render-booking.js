export function renderBookingPlaceholder(selector) {
  const el = document.querySelector(selector);
  if (!el) return;
  el.innerHTML = `
    <div class="panel">
      <h3>Booking Flow</h3>
      <p>Booking stepper wiring will be connected to simulation state.</p>
    </div>
  `;
}
