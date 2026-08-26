document.addEventListener("click", (event) => {
    const resultButton = event.target.closest("[data-result-toggle-button]");
    if (resultButton) {
        event.stopPropagation();
        toggleResultDetail(resultButton.dataset.resultToggleButton);
        return;
    }

    const resultRow = event.target.closest("[data-result-toggle]");
    if (resultRow) {
        toggleResultDetail(resultRow.dataset.resultToggle);
        return;
    }

    const trigger = event.target.closest("[data-kvkk-toggle]");
    if (trigger) {
        const text = document.querySelector("[data-kvkk-text]");
        if (text) {
            text.hidden = !text.hidden;
        }
    }

    const copyButton = event.target.closest("[data-copy-button]");
    if (copyButton) {
        const target = copyButton.dataset.copyTarget;
        const source = target
            ? document.querySelector(`[data-copy-source="${target}"]`)
            : document.querySelector("[data-copy-source]");
        if (source) {
            source.select();
            navigator.clipboard?.writeText(source.value);
        }
    }
});

document.addEventListener("keydown", (event) => {
    if (event.key !== "Enter" && event.key !== " ") {
        return;
    }

    const resultRow = event.target.closest("[data-result-toggle]");
    if (!resultRow) {
        return;
    }

    event.preventDefault();
    toggleResultDetail(resultRow.dataset.resultToggle);
});

function toggleResultDetail(detailId) {
    if (!detailId) {
        return;
    }

    const detail = document.getElementById(detailId);
    if (!detail) {
        return;
    }

    detail.hidden = !detail.hidden;
    document.querySelectorAll(`[data-result-toggle="${detailId}"]`).forEach((row) => {
        row.classList.toggle("is-open", !detail.hidden);
    });
}

document.querySelectorAll("[data-general-survey]").forEach((generalSurveyCheckbox) => {
    const form = generalSurveyCheckbox.closest("form") ?? document;
    const targetedFields = form.querySelector("[data-targeted-fields]");
    if (!targetedFields) {
        return;
    }

    const syncSurveyScopeFields = () => {
        const disabled = generalSurveyCheckbox.checked;
        targetedFields.classList.toggle("is-hidden", disabled);
        targetedFields.querySelectorAll("select").forEach((select) => {
            select.disabled = disabled;
            if (disabled) {
                select.value = "";
            }
        });
    };

    generalSurveyCheckbox.addEventListener("change", syncSurveyScopeFields);
    syncSurveyScopeFields();
});

document.querySelectorAll("[data-department-select]").forEach((departmentSelect) => {
    const form = departmentSelect.closest("form") ?? document;
    const doctorSelect = form.querySelector("[data-doctor-select]");
    if (!doctorSelect) {
        return;
    }

    departmentSelect.addEventListener("change", async () => {
        doctorSelect.innerHTML = '<option value="">Doktor seçin</option>';
        if (!departmentSelect.value) {
            return;
        }

        const url = `${departmentSelect.dataset.doctorsUrl}?departmentId=${departmentSelect.value}`;
        const response = await fetch(url, { headers: { "Accept": "application/json" } });
        const doctors = await response.json();
        doctors.forEach((doctor) => {
            const option = document.createElement("option");
            option.value = doctor.id;
            option.textContent = doctor.displayName;
            doctorSelect.appendChild(option);
        });
    });
});

const userRoleSelect = document.querySelector("[data-user-role-select]");
const doctorProfileFields = document.querySelector("[data-doctor-profile-fields]");
const patientPiiPermission = document.querySelector("[data-patient-pii-permission]");

function syncDoctorProfileFields() {
    if (!userRoleSelect) {
        return;
    }

    const doctorRoleId = userRoleSelect.dataset.doctorRoleId;
    const isDoctor = Boolean(doctorRoleId) && userRoleSelect.value === doctorRoleId;

    if (doctorProfileFields) {
        doctorProfileFields.classList.toggle("is-hidden", !isDoctor);
        doctorProfileFields.querySelectorAll("input, select").forEach((field) => {
            field.disabled = !isDoctor;
            if (!isDoctor) {
                field.value = "";
            }
        });
    }

    if (patientPiiPermission) {
        patientPiiPermission.disabled = isDoctor;
        if (isDoctor) {
            patientPiiPermission.checked = false;
        }
    }
}

userRoleSelect?.addEventListener("change", syncDoctorProfileFields);
syncDoctorProfileFields();

document.querySelectorAll("[data-permission-toggle-form]").forEach((form) => {
    const checkbox = form.querySelector("[data-permission-toggle-checkbox]");
    const submitButton = form.querySelector("[data-permission-toggle-submit]");
    if (!checkbox || !submitButton) {
        return;
    }

    const initialChecked = checkbox.dataset.initialChecked === "true";
    const syncPermissionSubmit = () => {
        const isChanged = checkbox.checked !== initialChecked;
        submitButton.hidden = !isChanged;
        submitButton.disabled = !isChanged;
    };

    checkbox.addEventListener("change", syncPermissionSubmit);
    syncPermissionSubmit();
});

document.querySelectorAll("[data-survey-question-editor]").forEach((editor) => {
    const list = editor.querySelector("[data-survey-question-list]");
    const template = editor.querySelector("[data-survey-question-template]");
    const addButton = editor.querySelector("[data-add-survey-question]");

    if (!list || !template || !addButton) {
        return;
    }

    const refreshRows = () => {
        const rows = Array.from(list.querySelectorAll("[data-survey-question-row]"));
        rows.forEach((row, index) => {
            const number = index + 1;
            const title = row.querySelector(".question-row-header strong");
            const orderInput = row.querySelector("[data-question-order]");
            const removeButton = row.querySelector("[data-remove-survey-question]");

            if (title) {
                title.textContent = `Soru ${number}`;
            }

            if (orderInput) {
                orderInput.value = number;
            }

            if (removeButton) {
                removeButton.disabled = rows.length === 1;
            }

            row.querySelectorAll("[name]").forEach((field) => {
                field.name = field.name.replace(/Questions\[\d+\]/g, `Questions[${index}]`);
            });

            row.querySelectorAll("[id]").forEach((field) => {
                field.id = field.id.replace(/Questions_\d+__/g, `Questions_${index}__`);
            });

            row.querySelectorAll("label[for]").forEach((label) => {
                label.htmlFor = label.htmlFor.replace(/Questions_\d+__/g, `Questions_${index}__`);
            });
        });
    };

    addButton.addEventListener("click", () => {
        const index = list.querySelectorAll("[data-survey-question-row]").length;
        const number = index + 1;
        const wrapper = document.createElement("div");
        wrapper.innerHTML = template.innerHTML
            .replaceAll("__index__", index.toString())
            .replaceAll("__number__", number.toString())
            .trim();

        const row = wrapper.firstElementChild;
        if (row) {
            list.appendChild(row);
            refreshRows();
            row.querySelector("textarea")?.focus();
        }
    });

    list.addEventListener("click", (event) => {
        const removeButton = event.target.closest("[data-remove-survey-question]");
        if (!removeButton) {
            return;
        }

        const rows = list.querySelectorAll("[data-survey-question-row]");
        const row = removeButton.closest("[data-survey-question-row]");
        if (rows.length === 1) {
            row?.querySelectorAll("textarea").forEach((textarea) => {
                textarea.value = "";
            });
            return;
        }

        row?.remove();
        refreshRows();
    });

    refreshRows();
});
