using System;
using System.Collections.Generic;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>
/// The built-in slash commands of Claude Code that Conversations suggests, and those that open one of its
/// menus. A menu exists only in the session's terminal, so ClaudeSessions shows that tab in front for it.
/// </summary>
public static class SlashCommands
{
    /// <summary>The built-in commands suggested while typing a slash, by name.</summary>
    public static readonly IReadOnlyList<SkillInfo> BuiltIn =
    [
        new("add-dir", "Ajouter un dossier de travail à la session"),
        new("agents", "Gérer les sous-agents (menu)"),
        new("clear", "Effacer la conversation et repartir de zéro"),
        new("compact", "Résumer la conversation pour libérer le contexte"),
        new("config", "Ouvrir les réglages (menu)"),
        new("context", "Voir l'occupation du contexte"),
        new("cost", "Voir le coût de la session"),
        new("doctor", "Vérifier l'installation (menu)"),
        new("effort", "Régler l'effort du modèle (menu sans argument)"),
        new("exit", "Quitter la session"),
        new("export", "Exporter la conversation (menu)"),
        new("fast", "Activer ou couper le mode rapide"),
        new("feedback", "Envoyer un commentaire à Anthropic (menu)"),
        new("help", "Afficher l'aide"),
        new("hooks", "Gérer les hooks (menu)"),
        new("ide", "Se connecter à un éditeur (menu)"),
        new("init", "Créer un CLAUDE.md pour le projet"),
        new("install-github-app", "Installer l'app GitHub (menu)"),
        new("login", "Se connecter (menu)"),
        new("logout", "Se déconnecter"),
        new("mcp", "Gérer les serveurs MCP (menu)"),
        new("memory", "Modifier les fichiers de mémoire (menu)"),
        new("model", "Choisir le modèle (menu sans argument)"),
        new("output-style", "Changer le style des réponses (menu sans argument)"),
        new("permissions", "Gérer les permissions (menu)"),
        new("plugin", "Gérer les plugins (menu)"),
        new("privacy-settings", "Régler la confidentialité (menu)"),
        new("resume", "Reprendre une autre session (menu)"),
        new("review", "Revoir une pull request"),
        new("rewind", "Revenir à un message précédent (menu)"),
        new("status", "Voir l'état de la session (menu)"),
        new("tasks", "Voir les tâches en arrière-plan (menu)"),
        new("terminal-setup", "Configurer le terminal (menu)"),
        new("theme", "Changer le thème (menu)"),
        new("upgrade", "Changer d'abonnement (menu)"),
        new("usage", "Voir l'utilisation et les limites du forfait (menu)"),
    ];

    private static readonly HashSet<string> AlwaysMenu = new(StringComparer.Ordinal)
    {
        "agents", "config", "doctor", "export", "feedback", "hooks", "ide", "install-github-app", "login", "mcp", "memory",
        "permissions", "plugin", "privacy-settings", "resume", "rewind", "status", "tasks", "terminal-setup", "theme", "upgrade", "usage",
    };

    private static readonly HashSet<string> MenuWithoutArguments = new(StringComparer.Ordinal) { "effort", "model", "output-style" };

    /// <summary>Tells whether a message is a slash command that opens a menu of Claude Code.</summary>
    /// <param name="message">The message, as sent.</param>
    /// <returns>True for such a command, such as /rewind, or /model without a model.</returns>
    public static bool OpensMenu(string message)
    {
        string text = message.Trim();
        if (!text.StartsWith('/') || text.Contains('\n', StringComparison.Ordinal))
        {
            return false;
        }
        int space = text.IndexOf(' ', StringComparison.Ordinal);
        string name = space < 0 ? text[1..] : text[1..space];
        bool hasArguments = space >= 0 && text[(space + 1)..].Trim().Length > 0;
        return AlwaysMenu.Contains(name) || (MenuWithoutArguments.Contains(name) && !hasArguments);
    }
}
