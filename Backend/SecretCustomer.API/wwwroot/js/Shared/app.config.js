// Application Configuration
// Bu dosyayi production'da ENVIRONMENT = 'Production' olarak degistirin
var APP_CONFIG = {
    // 'Development' veya 'Production'
    ENVIRONMENT: 'Development',

    // Development modunda detayli hata mesajlari gosterilir
    // Production modunda sadece ozet mesaj gosterilir
    isProduction: function() {
        return this.ENVIRONMENT === 'Production';
    },

    isDevelopment: function() {
        return this.ENVIRONMENT === 'Development';
    }
};
