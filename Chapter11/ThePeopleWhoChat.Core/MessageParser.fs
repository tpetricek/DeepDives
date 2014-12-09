namespace ThePeopleWhoChat.Core

    open System.Text.RegularExpressions

    type MessageParser() =

        static let urlregex =
            let scheme = "(https?\:\/\/)?"
            let textdomain = "([\da-zA-Z-]+)(\.[a-zA-Z]+)+"
            let ipdomain = "((\d{1,3}\.){3}(\d{1,3}))"
            let domain = sprintf "(%s|%s)" textdomain ipdomain
            let port = "(\:[\d]+)?"
            let path = "(\/[^\s]*)?"
            let namedgroup = sprintf "(?<url>%s%s%s%s)" scheme domain port path
            new Regex(namedgroup,RegexOptions.Compiled)

        static member Parse(s:string) = 
            let replace (m:Match) =
                let url = m.Groups.["url"].Value
                let replUrl = if url.StartsWith("http") then url else sprintf "http://%s" url
                sprintf "<a href=\"%s\">%s</a>" replUrl url
            urlregex.Replace(s, replace)
