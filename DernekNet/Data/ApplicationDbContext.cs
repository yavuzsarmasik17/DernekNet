using DernekNet.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DernekNet.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Dernek> Dernekler { get; set; }
        public DbSet<Duyuru> Duyurular { get; set; }
        public DbSet<Etkinlik> Etkinlikler { get; set; }
        public DbSet<Uye> Uyeler { get; set; }
        public DbSet<YonetimKurulu> YonetimKuruluUyeleri { get; set; }
        public DbSet<DuyuruMedya> DuyuruMedyalari { get; set; }
        public DbSet<EtkinlikMedya> EtkinlikMedyalari { get; set; }
        public DbSet<Anket> Anketler { get; set; }
        public DbSet<AnketSecenek> AnketSecenekleri { get; set; }
        public DbSet<AnketOy> AnketOylari { get; set; }

        public DbSet<AfadGonullu> AfadGonulluler { get; set; }
        public DbSet<AfadHaber> AfadHaberler { get; set; }
        public DbSet<AfadDuyuru> AfadDuyurular { get; set; }
        public DbSet<AfadGorev> AfadGorevler { get; set; }
        public DbSet<AfadGorevAtama> AfadGorevAtamalari { get; set; }
        public DbSet<AfadEgitim> AfadEgitimler { get; set; }
        public DbSet<AfadEgitimKatilim> AfadEgitimKatilimlari { get; set; }
        public DbSet<AfadTatbikat> AfadTatbikatlar { get; set; }
        public DbSet<AfadTatbikatKatilim> AfadTatbikatKatilimlari { get; set; }
        public DbSet<AfadYetkinlik> AfadYetkinlikler { get; set; }
        public DbSet<AfadGonulluYetkinlik> AfadGonulluYetkinlikleri { get; set; }

        public DbSet<AfadEkip> AfadEkipler { get; set; }
        public DbSet<AfadEkipUye> AfadEkipUyeleri { get; set; }





        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // AFAD EKİPLERİ
            builder.Entity<AfadEkip>()
                .HasOne(x => x.Dernek)
                .WithMany()
                .HasForeignKey(x => x.DernekId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AfadEkip>()
                .HasIndex(x => new { x.DernekId, x.Ad })
                .IsUnique();

            // AFAD EKİP ÜYELERİ
            builder.Entity<AfadEkipUye>()
                .HasOne(x => x.Ekip)
                .WithMany(x => x.Uyeler)
                .HasForeignKey(x => x.AfadEkipId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AfadEkipUye>()
                .HasOne(x => x.Gonullu)
                .WithMany(x => x.EkipUyelikleri)
                .HasForeignKey(x => x.AfadGonulluId)
                .OnDelete(DeleteBehavior.Restrict);

            // Aynı gönüllü aynı ekibe iki kez eklenemez.
            builder.Entity<AfadEkipUye>()
                .HasIndex(x => new
                {
                    x.AfadEkipId,
                    x.AfadGonulluId
                })
                .IsUnique();

            // Bir ekipte en fazla bir aktif lider bulunabilir.
            builder.Entity<AfadEkipUye>()
                .HasIndex(x => x.AfadEkipId)
                .IsUnique()
                .HasFilter("[EkipLideriMi] = 1 AND [AktifMi] = 1");


            builder.Entity<Anket>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AnketSecenek>().HasOne(x => x.Anket).WithMany(x => x.Secenekler).HasForeignKey(x => x.AnketId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<AnketOy>().HasOne(x => x.Anket).WithMany(x => x.Oylar).HasForeignKey(x => x.AnketId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<AnketOy>().HasOne(x => x.AnketSecenek).WithMany(x => x.Oylar).HasForeignKey(x => x.AnketSecenekId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<AnketOy>().HasIndex(x => new { x.AnketId, x.ZiyaretciAnahtariHash }).IsUnique();
            builder.Entity<YonetimKurulu>().HasOne(x => x.Uye).WithMany().HasForeignKey(x => x.UyeId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<YonetimKurulu>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AfadGonullu>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadGonullu>().HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadGonullu>().HasIndex(x => x.KullaniciId).IsUnique(); // Bir hesap, tek derneğin AFAD birimi

            builder.Entity<AfadHaber>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadDuyuru>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadGorev>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadEgitim>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadTatbikat>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadYetkinlik>().HasOne(x => x.Dernek).WithMany().HasForeignKey(x => x.DernekId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AfadGorevAtama>().HasOne(x => x.Gorev).WithMany(x => x.Atamalar).HasForeignKey(x => x.AfadGorevId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadGorevAtama>().HasOne(x => x.Gonullu).WithMany(x => x.GorevAtamalari).HasForeignKey(x => x.AfadGonulluId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadGorevAtama>().HasIndex(x => new { x.AfadGorevId, x.AfadGonulluId }).IsUnique();

            builder.Entity<AfadEgitimKatilim>().HasOne(x => x.Egitim).WithMany(x => x.Katilimlar).HasForeignKey(x => x.AfadEgitimId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadEgitimKatilim>().HasOne(x => x.Gonullu).WithMany(x => x.EgitimKatilimlari).HasForeignKey(x => x.AfadGonulluId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadEgitimKatilim>().HasIndex(x => new { x.AfadEgitimId, x.AfadGonulluId }).IsUnique();

            builder.Entity<AfadTatbikatKatilim>().HasOne(x => x.Tatbikat).WithMany(x => x.Katilimlar).HasForeignKey(x => x.AfadTatbikatId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadTatbikatKatilim>().HasOne(x => x.Gonullu).WithMany(x => x.TatbikatKatilimlari).HasForeignKey(x => x.AfadGonulluId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadTatbikatKatilim>().HasIndex(x => new { x.AfadTatbikatId, x.AfadGonulluId }).IsUnique();

            builder.Entity<AfadGonulluYetkinlik>().HasOne(x => x.Gonullu).WithMany(x => x.Yetkinlikleri).HasForeignKey(x => x.AfadGonulluId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadGonulluYetkinlik>().HasOne(x => x.Yetkinlik).WithMany(x => x.Gonulluler).HasForeignKey(x => x.AfadYetkinlikId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AfadGonulluYetkinlik>().HasIndex(x => new { x.AfadGonulluId, x.AfadYetkinlikId }).IsUnique();
        }
    }
}
