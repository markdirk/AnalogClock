using System.Reflection;
using Avalonia.Controls;

namespace AnalogClock;

public partial class InfoWindow : Window
{
    public InfoWindow()
    {
        InitializeComponent();
        var version = typeof(InfoWindow).Assembly.GetName().Version?.ToString(3) ?? "1.4.3";
        EulaText!.Text = $@"Lizenzbedingungen / Endbenutzer-Lizenzvereinbarung (EULA)

Lizenzgeber: Marc Warzecha
Lizenzgegenstand ist die Software AnalogClock Version {version} bestehend aus:
1. einer digitalen Analoguhr, die die aktuelle Systemzeit anzeigt,
2. einer Alarmfunktion, die das Auslösen von Alarmen zu frei definierbaren Zeitpunkten ermöglicht,
3. einem zeitgesteuerten Programmstarter, der zu festgelegten Zeiten externe Programme automatisch ausführt und dabei optional benutzerdefinierte Startparameter übergeben kann.
Die Software ist für den Einsatz unter Windows 11 vorgesehen.

1. Vertragsgegenstand
Der Lizenzgeber stellt dem Lizenznehmer die Software zur Nutzung auf Windows-11-Systemen bereit. Die Software wird nicht verkauft, sondern ausschließlich lizenziert. Ein Eigentumsübergang findet nicht statt. Alle Rechte verbleiben beim Lizenzgeber.

2. Lizenzgewährung
Mit Zahlung der vereinbarten Nutzungsgebühr erhält der Lizenznehmer eine dauerhafte, nicht-exklusive, nicht übertragbare und nicht unterlizenzierbare Lizenz zur Nutzung der Software auf einem einzigen Computer.
Die Lizenz berechtigt ausschließlich zur Nutzung gemäß diesen Bedingungen.

3. Hardwaregebundener Lizenzschlüssel
Die Aktivierung der Software erfolgt über einen hardwaregebundenen Lizenzschlüssel, der eindeutig mit der Hardware desjenigen Computers verknüpft wird, auf dem die Software erstmals aktiviert wird.
Eine Nutzung der Software ist ausschließlich auf diesem Gerät zulässig.

4. Nutzung auf anderen Geräten / Hardwareänderungen
Die Lizenz gilt nur für die ursprüngliche Hardwarekonfiguration.
Eine Nutzung auf einem anderen Gerät oder nach einer wesentlichen Hardwareänderung (z. B. Austausch von CPU, Mainboard oder anderen lizenzrelevanten Komponenten) ist nicht zulässig und führt zum Erlöschen der Lizenz.
Für die Nutzung auf einem anderen oder geänderten Gerät ist der Erwerb einer neuen Lizenz erforderlich.

5. Updates und Weiterentwicklung
Der Lizenzgeber ist nicht verpflichtet, regelmäßige Updates, Funktions­erweiterungen oder Fehlerbehebungen bereitzustellen.
Bereitgestellte Updates erfolgen freiwillig und ohne Rechtsanspruch.
Es besteht kein Anspruch auf zukünftige Versionen oder Kompatibilität mit zukünftigen Windows-Versionen.

6. Weitergabe und Weiterlizenzierung
Der Lizenznehmer darf die Software oder den Lizenzschlüssel nicht:
- weitergeben,
- verkaufen,
- vermieten,
- verleihen,
- veröffentlichen,
- oder Dritten zugänglich machen.
Eine Weiterlizenzierung ist ausdrücklich untersagt.

7. Urheberrecht und geistiges Eigentum
Alle Urheberrechte, Verwertungsrechte, Bearbeitungsrechte und das Recht zur Weiterlizenzierung verbleiben vollständig beim Lizenzgeber.
Der Lizenznehmer erhält ausschließlich ein Nutzungsrecht gemäß dieser Vereinbarung.

8. Gewährleistung und Haftung
Die Software wird ""wie bereitgestellt"" geliefert.
Der Lizenzgeber übernimmt keine Gewähr für:
- Fehlerfreiheit,
- bestimmte Funktionen,
- Kompatibilität mit zukünftigen Systemen,
- oder dauerhafte Verfügbarkeit.
Die Haftung für Schäden, die aus Nutzung oder Fehlfunktion entstehen, ist ausgeschlossen, soweit gesetzlich zulässig. Zwingende Haftung nach deutschem Recht (z. B. bei Vorsatz oder grober Fahrlässigkeit) bleibt unberührt.

9. Schlussbestimmungen
Es gilt das Recht der Bundesrepublik Deutschland unter Ausschluss des UN-Kaufrechts.
Sollten einzelne Bestimmungen dieser Lizenz unwirksam sein, bleibt die Wirksamkeit der übrigen Bestimmungen unberührt.
Mit Installation oder Aktivierung der Software erkennt der Lizenznehmer diese Lizenzbedingungen an.";
    }
}
