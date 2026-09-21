(() => {
  const productSelect = document.getElementById("ProductId");
  const servicesGrid = document.getElementById("services-grid");
  const addServiceButton = document.getElementById("add-service-row");
  const serviceRowTemplate = document.getElementById("service-row-template");

  const getServiceRows = () =>
    servicesGrid ? [...servicesGrid.querySelectorAll("[data-service-row]")] : [];

  const fillServiceOptions = (select, services, selectedValue = "") => {
    select.innerHTML = '<option value="">Select service</option>';
    services.forEach((service) => {
      const option = document.createElement("option");
      option.value = service.id;
      option.textContent = `${service.name} (${service.code})`;
      if (service.id === selectedValue) {
        option.selected = true;
      }
      select.appendChild(option);
    });
  };

  const loadProductServices = async () => {
    if (!productSelect?.value) {
      return [];
    }

    const response = await fetch(`/api/internal/services?productId=${productSelect.value}`);
    if (!response.ok) {
      return [];
    }

    return response.json();
  };

  const reindexServiceRows = () => {
    getServiceRows().forEach((row, index) => {
      row.querySelector(".service-row-title").textContent = `Service ${index + 1}`;

      row.querySelectorAll("input, select").forEach((input) => {
        if (!input.name) {
          return;
        }

        input.name = input.name.replace(/Services\[\d+\]/, `Services[${index}]`);
        if (input.id) {
          input.id = input.id.replace(/Services_\d+__/, `Services_${index}__`);
        }
      });

      const dbCheck = row.querySelector("[data-db-check], input[name$='.DatabaseChanges']");
      const dbLabel = row.querySelector("[data-db-label], label[for*='DatabaseChanges']");
      if (dbCheck && dbLabel) {
        dbCheck.id = `Services_${index}__DatabaseChanges`;
        dbLabel.setAttribute("for", dbCheck.id);
      }

      const cfgCheck = row.querySelector("[data-cfg-check], input[name$='.ConfigurationChanges']");
      const cfgLabel = row.querySelector("[data-cfg-label], label[for*='ConfigurationChanges']");
      if (cfgCheck && cfgLabel) {
        cfgCheck.id = `Services_${index}__ConfigurationChanges`;
        cfgLabel.setAttribute("for", cfgCheck.id);
      }
    });
  };

  const refreshRemoveButtons = () => {
    const rows = getServiceRows();
    rows.forEach((row) => {
      const button = row.querySelector("[data-remove-service]");
      if (button) {
        button.disabled = rows.length <= 1;
      }
    });
  };

  const addServiceRow = async () => {
    if (!servicesGrid || !serviceRowTemplate) {
      return;
    }

    const fragment = serviceRowTemplate.content.cloneNode(true);
    const row = fragment.querySelector("[data-service-row]");
    servicesGrid.appendChild(row);
    reindexServiceRows();
    refreshRemoveButtons();

    const services = await loadProductServices();
    const select = row.querySelector("select[name$='.ServiceId']");
    if (select && services.length) {
      fillServiceOptions(select, services);
    }
  };

  if (productSelect) {
    productSelect.addEventListener("change", async () => {
      const services = await loadProductServices();
      document.querySelectorAll("select[name$='.ServiceId']").forEach((select) => {
        fillServiceOptions(select, services, select.value);
      });
    });
  }

  if (addServiceButton) {
    addServiceButton.addEventListener("click", (event) => {
      event.preventDefault();
      addServiceRow();
    });
  }

  if (servicesGrid) {
    servicesGrid.addEventListener("click", (event) => {
      const button = event.target.closest("[data-remove-service]");
      if (!button) {
        return;
      }

      event.preventDefault();
      if (getServiceRows().length <= 1) {
        return;
      }

      button.closest("[data-service-row]")?.remove();
      reindexServiceRows();
      refreshRemoveButtons();
    });

    refreshRemoveButtons();
  }

  // ---- Procedure v4.0: source links, category preview, expedited fields ----
  const referencesGrid = document.getElementById("references-grid");
  const addReferenceButton = document.getElementById("add-reference-row");
  const referenceTemplate = document.getElementById("reference-row-template");

  const reindexReferenceRows = () => {
    if (!referencesGrid) {
      return;
    }

    [...referencesGrid.querySelectorAll("[data-reference-row]")].forEach((row, index) => {
      row.querySelectorAll("input, select").forEach((input) => {
        if (input.name) {
          input.name = input.name.replace(/References\[[^\]]+\]/, `References[${index}]`);
        }
      });
    });
  };

  if (addReferenceButton && referencesGrid && referenceTemplate) {
    addReferenceButton.addEventListener("click", (event) => {
      event.preventDefault();
      const fragment = referenceTemplate.content.cloneNode(true);
      referencesGrid.appendChild(fragment.querySelector("[data-reference-row]"));
      reindexReferenceRows();
    });

    referencesGrid.addEventListener("click", (event) => {
      const button = event.target.closest("[data-remove-reference]");
      if (!button) {
        return;
      }

      event.preventDefault();
      if (referencesGrid.querySelectorAll("[data-reference-row]").length <= 1) {
        button.closest("[data-reference-row]").querySelectorAll("input").forEach((input) => { input.value = ""; });
        return;
      }

      button.closest("[data-reference-row]")?.remove();
      reindexReferenceRows();
    });
  }

  const categoryPreview = document.getElementById("category-preview");
  const downtimeRequired = document.getElementById("downtime-required");
  const updateCategoryPreview = () => {
    if (!categoryPreview) {
      return;
    }

    const majorChecked = [...document.querySelectorAll("input[name='Criteria'][data-criterion='major']")].some((input) => input.checked);
    const normalChecked = [...document.querySelectorAll("input[name='Criteria'][data-criterion='normal']")].some((input) => input.checked);
    const dbChanges = [...document.querySelectorAll("input[name$='.DatabaseChanges']")].some((input) => input.checked);
    const downtime = downtimeRequired?.checked ?? false;

    categoryPreview.textContent = majorChecked ? "Major" : (normalChecked || dbChanges || downtime ? "Normal" : "Minor");
  };

  document.addEventListener("change", (event) => {
    if (event.target.matches("input[name='Criteria'], input[name$='.DatabaseChanges'], #downtime-required")) {
      updateCategoryPreview();
    }
  });
  updateCategoryPreview();

  const executionMode = document.getElementById("execution-mode");
  const expeditedFields = document.getElementById("expedited-fields");
  if (executionMode && expeditedFields) {
    const toggle = () => {
      const selected = executionMode.options[executionMode.selectedIndex]?.text ?? "";
      expeditedFields.classList.toggle("d-none", selected !== "Expedited");
    };

    executionMode.addEventListener("change", toggle);
    toggle();
  }

  const bell = document.getElementById("notification-bell");
  const list = document.getElementById("notification-list");
  const badge = document.getElementById("notification-count");
  if (bell && list && badge) {
    const load = async () => {
      const response = await fetch("/Notifications/Latest");
      if (!response.ok) {
        return;
      }

      const payload = await response.json();
      badge.textContent = payload.unread > 0 ? String(payload.unread) : "";
      badge.style.display = payload.unread > 0 ? "inline-block" : "none";
      list.innerHTML = "";

      if (!payload.items.length) {
        list.innerHTML = '<div class="dropdown-item-text text-muted">No notifications</div>';
        return;
      }

      payload.items.forEach((item) => {
        const row = document.createElement("div");
        row.className = "dropdown-item-text border-bottom py-2";
        row.innerHTML = `<strong>${item.title}</strong><div class="small text-muted">${item.message}</div>`;
        list.appendChild(row);
      });
    };

    load();
    setInterval(load, 60000);
  }
})();
