document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".dn-metin-aktar").forEach(kutu => {
        const input = kutu.querySelector(".dn-metin-dosya");
        const durum = kutu.querySelector(".dn-metin-durum");
        const form = kutu.closest("form");
        const metinAlani = form && form.querySelector('textarea[name="Icerik"]');
        if (!input || !form || !metinAlani) return;
        input.addEventListener("change", async () => {
            const dosya = input.files && input.files[0];
            if (!dosya) return;
            if (dosya.size > 5 * 1024 * 1024) { durum.textContent = "Dosya en fazla 5 MB olabilir."; input.value = ""; return; }
            if (!/\.(txt|docx|pdf)$/i.test(dosya.name)) { durum.textContent = "TXT, DOCX veya PDF seçin."; input.value = ""; return; }
            if (metinAlani.value.trim() && !confirm("Mevcut açıklama metninin yerine dosyadaki metin yazılsın mı?")) { input.value = ""; return; }
            const token = form.querySelector('input[name="__RequestVerificationToken"]');
            const body = new FormData(); body.append("dosya", dosya);
            if (token) body.append("__RequestVerificationToken", token.value);
            input.disabled = true;
            durum.textContent = "Dosya okunuyor, lütfen bekleyin...";
            try {
                const yanit = await fetch("/DernekAdmin/DuyuruMetinAktar/Oku", { method: "POST", body, credentials: "same-origin" });
                const sonuc = await yanit.json().catch(() => ({}));
                if (!yanit.ok) throw new Error(sonuc.hata || "Dosya aktarılamadı (" + yanit.status + ").");
                metinAlani.value = sonuc.metin;
                metinAlani.dispatchEvent(new Event("input", { bubbles: true }));
                durum.textContent = "Metin aktarıldı. Kontrol edip duyuruyu kaydedebilirsiniz.";
                metinAlani.focus();
            } catch (hata) { durum.textContent = hata.message || "Dosya okunamadı."; }
            finally { input.disabled = false; input.value = ""; }
        });
    });
});